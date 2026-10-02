using PokerTrackerApi.Contract;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.InternalRepresentation.Events;

namespace PokerTrackerApi.HandReplays;

public interface IHandReplayMapper
{
    HandReplayDto Map(HandReplay replay);
}

public class HandReplayMapper : IHandReplayMapper
{
    public HandReplayDto Map(HandReplay replay)
    {
        var players = replay.Players.OrderBy(player => player.Position).ToArray();
        var playersById = players.ToDictionary(player => player.PlayerId);
        var remainingStacks = players.ToDictionary(
            player => player.PlayerId,
            player => player.StartingStackBB
        );
        var activePlayerIds = players.Select(player => player.PlayerId).ToHashSet();
        var pendingPlayerIds = activePlayerIds.ToHashSet();
        var streetBets = new Dictionary<string, decimal>();
        var revealedHoleCards = new Dictionary<string, HoleCardsDto>();
        var winnings = new Dictionary<string, decimal>();
        var board = new List<PlayingCardDto>();
        var actionSequence = new List<HandReplaySpotDto>();
        var pot = 0m;
        var street = "Preflop";
        var nextToAct = FindNextToAct(PokerPosition.BB);
        var hasInitialSnapshot = false;

        void AddSnapshot()
        {
            actionSequence.Add(
                new HandReplaySpotDto(
                    pot,
                    players
                        .Where(player => activePlayerIds.Contains(player.PlayerId))
                        .Select(player => player.Position.ToString())
                        .ToArray(),
                    nextToAct,
                    players
                        .Where(player => streetBets.ContainsKey(player.PlayerId))
                        .ToDictionary(
                            player => player.Position.ToString(),
                            player => streetBets[player.PlayerId]
                        ),
                    players.ToDictionary(
                        player => player.Position.ToString(),
                        player => remainingStacks[player.PlayerId]
                    ),
                    players
                        .Where(player => revealedHoleCards.ContainsKey(player.PlayerId))
                        .ToDictionary(
                            player => player.Position.ToString(),
                            player => revealedHoleCards[player.PlayerId]
                        ),
                    street,
                    board.ToArray(),
                    players
                        .Where(player => winnings.ContainsKey(player.PlayerId))
                        .ToDictionary(
                            player => player.Position.ToString(),
                            player => winnings[player.PlayerId]
                        )
                )
            );
        }

        void AddInitialSnapshot()
        {
            if (hasInitialSnapshot)
            {
                return;
            }

            nextToAct = FindNextToAct(PokerPosition.BB);
            AddSnapshot();
            hasInitialSnapshot = true;
        }

        void AddStreetBet(string playerId, decimal amount)
        {
            streetBets[playerId] = streetBets.GetValueOrDefault(playerId) + amount;
            remainingStacks[playerId] -= amount;
        }

        void SweepStreetBets()
        {
            pot += streetBets.Values.Sum();
            streetBets.Clear();
        }

        string? FindNextToAct(PokerPosition afterPosition)
        {
            var positionIndex = Array.FindIndex(
                players,
                player => player.Position == afterPosition
            );
            for (var offset = 1; offset <= players.Length; offset++)
            {
                var player = players[(positionIndex + offset) % players.Length];
                if (
                    activePlayerIds.Contains(player.PlayerId)
                    && pendingPlayerIds.Contains(player.PlayerId)
                    && remainingStacks[player.PlayerId] > 0m
                )
                {
                    return player.Position.ToString();
                }
            }

            return null;
        }

        foreach (var handEvent in replay.Events.OrderBy(handEvent => handEvent.Sequence))
        {
            if (handEvent.EventType is nameof(FlopDealt) or nameof(TurnDealt) or nameof(RiverDealt))
            {
                AddInitialSnapshot();
                SweepStreetBets();
                street = handEvent.Street;
                board.AddRange(
                    handEvent
                        .Cards.OrderBy(card => card.CardIndex)
                        .Select(card => MapCard(card.Card))
                );
                pendingPlayerIds = players
                    .Where(player =>
                        activePlayerIds.Contains(player.PlayerId)
                        && remainingStacks[player.PlayerId] > 0m
                    )
                    .Select(player => player.PlayerId)
                    .ToHashSet();
                nextToAct = FindNextToAct(PokerPosition.BTN);
                AddSnapshot();
                continue;
            }

            switch (handEvent.EventType)
            {
                case nameof(PostAnte):
                    if (handEvent.PlayerId is { } antePlayerId && handEvent.AmountBB is { } ante)
                    {
                        remainingStacks[antePlayerId] -= ante;
                        pot += ante;
                    }
                    break;
                case nameof(PostSmallBlind):
                case nameof(PostBigBlind):
                    if (handEvent.PlayerId is { } blindPlayerId && handEvent.AmountBB is { } blind)
                    {
                        AddStreetBet(blindPlayerId, blind);
                    }
                    break;
                case nameof(PlayerFoldEvent):
                case nameof(PlayerCheckEvent):
                case nameof(PlayerCallEvent):
                case nameof(PlayerBetEvent):
                case nameof(PlayerRaiseEvent):
                    AddInitialSnapshot();
                    if (handEvent.PlayerId is not { } actorId)
                    {
                        break;
                    }

                    pendingPlayerIds.Remove(actorId);
                    if (handEvent.EventType == nameof(PlayerFoldEvent))
                    {
                        activePlayerIds.Remove(actorId);
                    }
                    else if (handEvent.EventType == nameof(PlayerCallEvent))
                    {
                        AddStreetBet(actorId, handEvent.AmountBB ?? 0m);
                    }
                    else if (handEvent.EventType == nameof(PlayerBetEvent))
                    {
                        AddStreetBet(actorId, handEvent.AmountBB ?? 0m);
                        ResetPendingPlayers(actorId);
                    }
                    else if (handEvent.EventType == nameof(PlayerRaiseEvent))
                    {
                        var previousBet = streetBets.GetValueOrDefault(actorId);
                        var raiseTo =
                            handEvent.RaiseToAmountBB ?? previousBet + (handEvent.AmountBB ?? 0m);
                        AddStreetBet(actorId, raiseTo - previousBet);
                        ResetPendingPlayers(actorId);
                    }

                    nextToAct = FindNextToAct(playersById[actorId].Position);
                    AddSnapshot();
                    break;
                case nameof(UncalledBetReturned):
                    AddInitialSnapshot();
                    if (
                        handEvent.PlayerId is { } returnedPlayerId
                        && handEvent.AmountBB is { } returned
                    )
                    {
                        var amountReturned = Math.Min(
                            returned,
                            streetBets.GetValueOrDefault(returnedPlayerId)
                        );
                        streetBets[returnedPlayerId] -= amountReturned;
                        remainingStacks[returnedPlayerId] += amountReturned;
                        if (streetBets[returnedPlayerId] == 0m)
                        {
                            streetBets.Remove(returnedPlayerId);
                        }
                        AddSnapshot();
                    }
                    break;
                case nameof(CashDrop):
                    pot = Math.Max(0m, pot - (handEvent.AmountBB ?? 0m));
                    AddSnapshot();
                    break;
                case nameof(CardsShown):
                    AddInitialSnapshot();
                    var shownCards = handEvent
                        .Cards.OrderBy(card => card.CardIndex)
                        .Select(card => card.Card)
                        .Take(2)
                        .ToArray();
                    if (handEvent.PlayerId is { } shownPlayerId && shownCards.Length == 2)
                    {
                        revealedHoleCards[shownPlayerId] = MapHoleCards(
                            shownCards[0],
                            shownCards[1]
                        );
                        AddSnapshot();
                    }
                    break;
                case nameof(PotAwarded):
                    AddInitialSnapshot();
                    SweepStreetBets();
                    if (handEvent.PlayerId is { } winnerId && handEvent.AmountBB is { } award)
                    {
                        pot = Math.Max(0m, pot - award);
                        winnings[winnerId] = winnings.GetValueOrDefault(winnerId) + award;
                        remainingStacks[winnerId] += award;
                        AddSnapshot();
                    }
                    break;
            }
        }

        AddInitialSnapshot();

        return new HandReplayDto(
            replay.HandId,
            MapHoleCards(replay.HeroHoleCards.First, replay.HeroHoleCards.Second),
            replay.HeroPosition.ToString(),
            players.ToDictionary(
                player => player.Position.ToString(),
                player => player.StartingStackBB
            ),
            actionSequence
        );

        void ResetPendingPlayers(string actorId)
        {
            pendingPlayerIds = players
                .Where(player =>
                    player.PlayerId != actorId
                    && activePlayerIds.Contains(player.PlayerId)
                    && remainingStacks[player.PlayerId] > 0m
                )
                .Select(player => player.PlayerId)
                .ToHashSet();
        }
    }

    private static HoleCardsDto MapHoleCards(PlayingCard first, PlayingCard second) =>
        new(MapCard(first), MapCard(second));

    private static PlayingCardDto MapCard(PlayingCard card) =>
        new(card.Rank.ToCode(), card.Suit.ToCode());
}
