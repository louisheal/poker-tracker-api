using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Contract;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandReplays;

public interface IHandReplayRepository
{
    Task<HandReplay?> GetHandReplayAsync(string handId, CancellationToken cancellationToken);
}

public class HandReplayRepository : IHandReplayRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandReplayRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HandReplay?> GetHandReplayAsync(
        string handId,
        CancellationToken cancellationToken
    )
    {
        var pokerHand = await _dbContext
            .PokerHands.AsNoTracking()
            .Include(hand => hand.Players)
            .Include(hand => hand.Events)
            .SingleOrDefaultAsync(hand => hand.HandId == handId, cancellationToken);

        if (pokerHand is null)
        {
            return null;
        }

        return ToHandReplayDto(pokerHand);
    }

    private static HandReplay ToHandReplayDto(PokerHand hand)
    {
        var players = hand
            .Players.OrderBy(player => player.Position)
            .Select(player => new
            {
                player.PlayerId,
                player.Position,
                player.StartingStackBB,
            })
            .ToArray();
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
        var actionSequence = new List<HandReplaySpot>();
        var pot = 0m;
        var street = PokerStreet.Preflop.ToString();
        var nextToAct = FindNextToAct(PokerPosition.BB);
        var hasInitialSnapshot = false;

        void AddSnapshot()
        {
            actionSequence.Add(
                new HandReplaySpot(
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

        void AddBoardCards(PokerStreet dealtStreet, IReadOnlyList<PlayingCard> cards)
        {
            AddInitialSnapshot();
            SweepStreetBets();
            street = dealtStreet.ToString();
            board.AddRange(cards.Select(MapCard));
            pendingPlayerIds = players
                .Where(player =>
                    activePlayerIds.Contains(player.PlayerId)
                    && remainingStacks[player.PlayerId] > 0m
                )
                .Select(player => player.PlayerId)
                .ToHashSet();
            nextToAct = FindNextToAct(PokerPosition.BTN);
            AddSnapshot();
        }

        void ProcessPlayerAction(
            string actorId,
            bool isFold = false,
            decimal? callAmount = null,
            decimal? betAmount = null,
            decimal? raiseToAmount = null
        )
        {
            AddInitialSnapshot();
            pendingPlayerIds.Remove(actorId);
            if (isFold)
            {
                activePlayerIds.Remove(actorId);
            }
            else if (callAmount is { } call)
            {
                AddStreetBet(actorId, call);
            }
            else if (betAmount is { } bet)
            {
                AddStreetBet(actorId, bet);
                ResetPendingPlayers(actorId);
            }
            else if (raiseToAmount is { } raiseTo)
            {
                var previousBet = streetBets.GetValueOrDefault(actorId);
                AddStreetBet(actorId, raiseTo - previousBet);
                ResetPendingPlayers(actorId);
            }

            nextToAct = FindNextToAct(playersById[actorId].Position);
            AddSnapshot();
        }

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

        foreach (var handEvent in hand.Events.OrderBy(handEvent => handEvent.Sequence))
        {
            switch (handEvent)
            {
                case PokerHandFlopDealtEvent flop:
                    AddBoardCards(PokerStreet.Flop, [flop.First, flop.Second, flop.Third]);
                    break;
                case PokerHandTurnDealtEvent turn:
                    AddBoardCards(PokerStreet.Turn, [turn.Card]);
                    break;
                case PokerHandRiverDealtEvent river:
                    AddBoardCards(PokerStreet.River, [river.Card]);
                    break;
                case PokerHandAntePostEvent ante:
                    remainingStacks[ante.PlayerId] -= ante.AmountBB;
                    pot += ante.AmountBB;
                    break;
                case PokerHandSmallBlindPostEvent smallBlind:
                    AddStreetBet(smallBlind.PlayerId, smallBlind.AmountBB);
                    break;
                case PokerHandBigBlindPostEvent bigBlind:
                    AddStreetBet(bigBlind.PlayerId, bigBlind.AmountBB);
                    break;
                case PokerHandPlayerFoldEvent fold:
                    ProcessPlayerAction(fold.PlayerId, isFold: true);
                    break;
                case PokerHandPlayerCheckEvent check:
                    ProcessPlayerAction(check.PlayerId);
                    break;
                case PokerHandPlayerCallEvent call:
                    ProcessPlayerAction(call.PlayerId, callAmount: call.CallAmountBB);
                    break;
                case PokerHandPlayerBetEvent bet:
                    ProcessPlayerAction(bet.PlayerId, betAmount: bet.BetAmountBB);
                    break;
                case PokerHandPlayerRaiseEvent raise:
                    ProcessPlayerAction(raise.PlayerId, raiseToAmount: raise.RaiseToAmountBB);
                    break;
                case PokerHandUncalledBetReturnedEvent returned:
                    AddInitialSnapshot();
                    var amountReturned = Math.Min(
                        returned.AmountBB,
                        streetBets.GetValueOrDefault(returned.PlayerId)
                    );
                    streetBets[returned.PlayerId] -= amountReturned;
                    remainingStacks[returned.PlayerId] += amountReturned;
                    if (streetBets[returned.PlayerId] == 0m)
                    {
                        streetBets.Remove(returned.PlayerId);
                    }
                    AddSnapshot();
                    break;
                case PokerHandCashDropEvent cashDrop:
                    pot = Math.Max(0m, pot - cashDrop.AmountBB);
                    AddSnapshot();
                    break;
                case PokerHandCardsShownEvent cardsShown:
                    AddInitialSnapshot();
                    revealedHoleCards[cardsShown.PlayerId] = MapHoleCards(
                        cardsShown.HoleCards.First,
                        cardsShown.HoleCards.Second
                    );
                    AddSnapshot();
                    break;
                case PokerHandPotAwardedEvent award:
                    AddInitialSnapshot();
                    SweepStreetBets();
                    pot = Math.Max(0m, pot - award.AmountBB);
                    winnings[award.PlayerId] =
                        winnings.GetValueOrDefault(award.PlayerId) + award.AmountBB;
                    remainingStacks[award.PlayerId] += award.AmountBB;
                    AddSnapshot();
                    break;
            }
        }

        AddInitialSnapshot();

        return new HandReplay(
            hand.HandId,
            MapHoleCards(hand.HeroHoleCards.First, hand.HeroHoleCards.Second),
            playersById[hand.HeroPlayerId].Position.ToString(),
            players.ToDictionary(
                player => player.Position.ToString(),
                player => player.StartingStackBB
            ),
            actionSequence
        );
    }

    private static HoleCardsDto MapHoleCards(PlayingCard first, PlayingCard second) =>
        new(MapCard(first), MapCard(second));

    private static PlayingCardDto MapCard(PlayingCard card) =>
        new(card.Rank.ToCode(), card.Suit.ToCode());
}
