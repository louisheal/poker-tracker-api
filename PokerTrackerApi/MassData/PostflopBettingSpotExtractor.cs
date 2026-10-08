using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.MassData;

public static class PostflopBettingSpotExtractor
{
    public static IReadOnlyList<PostflopBettingSpot> Extract(PokerHand hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        var events = hand.Events.OrderBy(handEvent => handEvent.Sequence).ToArray();
        var players = hand.Players.ToDictionary(player => player.PlayerId);
        var activePlayers = players.Keys.ToHashSet(StringComparer.Ordinal);
        string? pfrPlayerId = null;
        var preflopRaiseCount = 0;
        var flopIndex = -1;
        Rank? flopHighCard = null;
        FlopTexture? flopTexture = null;

        for (var index = 0; index < events.Length; index++)
        {
            var handEvent = events[index];
            if (handEvent is PokerHandBoardDealtEvent)
            {
                if (handEvent is PokerHandFlopDealtEvent flopDealt)
                {
                    flopIndex = index;
                    flopHighCard = new[]
                    {
                        flopDealt.First.Rank,
                        flopDealt.Second.Rank,
                        flopDealt.Third.Rank,
                    }.Max();
                    flopTexture = GetFlopTexture(flopDealt);
                }

                break;
            }

            if (handEvent is PokerHandPlayerFoldEvent preflopFold)
            {
                activePlayers.Remove(preflopFold.PlayerId);
            }
            else if (handEvent is PokerHandPlayerRaiseEvent preflopRaise)
            {
                preflopRaiseCount++;
                pfrPlayerId = preflopRaise.PlayerId;
            }
            else if (handEvent is PokerHandPlayerBetEvent preflopBet)
            {
                preflopRaiseCount++;
                pfrPlayerId = preflopBet.PlayerId;
            }
        }

        if (
            flopIndex < 0
            || pfrPlayerId is null
            || !flopHighCard.HasValue
            || !flopTexture.HasValue
            || !activePlayers.Contains(pfrPlayerId)
            || activePlayers.Count != 2
        )
        {
            return [];
        }

        var defendingPlayerId = activePlayers.Single(playerId => playerId != pfrPlayerId);
        var pfr = players[pfrPlayerId];
        var defender = players[defendingPlayerId];
        var pfrInPosition = GetPostflopOrder(pfr.Position) > GetPostflopOrder(defender.Position);
        var spots = new List<PostflopBettingSpot>(2);
        var flopEnd = FindNextStreetIndex(events, flopIndex + 1);
        var flopActions = ReadStreetActions(
            events,
            flopIndex + 1,
            flopEnd,
            hand.HandId,
            hand.HeroPlayerId,
            hand.Timestamp,
            PokerStreet.Flop,
            pfrPlayerId,
            defendingPlayerId,
            pfr.Position,
            defender.Position,
            preflopRaiseCount,
            pfrInPosition,
            false,
            flopHighCard.Value,
            flopTexture.Value
        );

        if (flopActions.Spot is not null)
        {
            spots.Add(flopActions.Spot);
        }

        if (flopEnd < events.Length && events[flopEnd] is PokerHandTurnDealtEvent)
        {
            var turnEnd = FindNextStreetIndex(events, flopEnd + 1);
            var turnActions = ReadStreetActions(
                events,
                flopEnd + 1,
                turnEnd,
                hand.HandId,
                hand.HeroPlayerId,
                hand.Timestamp,
                PokerStreet.Turn,
                pfrPlayerId,
                defendingPlayerId,
                pfr.Position,
                defender.Position,
                preflopRaiseCount,
                pfrInPosition,
                flopActions.WentCheckCheck,
                flopHighCard.Value,
                flopTexture.Value
            );

            if (turnActions.Spot is not null)
            {
                spots.Add(turnActions.Spot);
            }
        }

        return spots;
    }

    private static StreetActionResult ReadStreetActions(
        IReadOnlyList<PokerHandEvent> events,
        int startIndex,
        int endIndex,
        string handId,
        string heroPlayerId,
        DateTimeOffset handTimestamp,
        PokerStreet street,
        string pfrPlayerId,
        string defendingPlayerId,
        PokerPosition pfrPosition,
        PokerPosition defendingPosition,
        int preflopRaiseCount,
        bool pfrInPosition,
        bool flopWentCheckCheck,
        Rank flopHighCard,
        FlopTexture flopTexture
    )
    {
        decimal? pfrBetBb = null;
        decimal? donkBetBb = null;
        PostflopResponseTo? responseTo = null;
        PostflopResponseAction? responseAction = null;
        decimal? responseAmountBb = null;
        string? pendingBettorId = null;
        var pfrActed = false;
        var defenderActed = false;
        var firstActions = new Dictionary<string, PokerHandPlayerActionEvent>(
            StringComparer.Ordinal
        );
        var sawBet = false;

        for (var index = startIndex; index < endIndex; index++)
        {
            if (events[index] is not PokerHandPlayerActionEvent action)
            {
                continue;
            }

            if (action.PlayerId != pfrPlayerId && action.PlayerId != defendingPlayerId)
            {
                continue;
            }

            firstActions.TryAdd(action.PlayerId, action);

            if (pendingBettorId is not null && action.PlayerId != pendingBettorId)
            {
                if (TryGetResponse(action, out var currentResponse, out var currentAmount))
                {
                    responseTo =
                        pendingBettorId == pfrPlayerId
                            ? PostflopResponseTo.PfrBet
                            : PostflopResponseTo.DonkBet;
                    responseAction = currentResponse;
                    responseAmountBb = currentAmount;
                    pendingBettorId = null;
                }

                continue;
            }

            var isPfr = action.PlayerId == pfrPlayerId;
            var hasActed = isPfr ? pfrActed : defenderActed;
            if (hasActed)
            {
                continue;
            }

            if (isPfr)
            {
                pfrActed = true;
                if (!sawBet && TryGetBetAmount(action, out var betAmount))
                {
                    pfrBetBb = betAmount;
                    if (betAmount > 0)
                    {
                        pendingBettorId = pfrPlayerId;
                        sawBet = true;
                    }
                }
            }
            else
            {
                defenderActed = true;
                if (!sawBet && TryGetBetAmount(action, out var betAmount))
                {
                    donkBetBb = betAmount;
                    if (betAmount > 0)
                    {
                        pendingBettorId = defendingPlayerId;
                        sawBet = true;
                    }
                }
            }
        }

        var wentCheckCheck =
            street == PokerStreet.Flop
            && firstActions.Count == 2
            && firstActions.Values.All(action => action is PokerHandPlayerCheckEvent)
            && !sawBet;

        var spot =
            pfrBetBb is null && donkBetBb is null
                ? null
                : new PostflopBettingSpot
                {
                    HandId = handId,
                    HeroPlayerId = heroPlayerId,
                    HandTimestamp = handTimestamp,
                    Street = street,
                    FlopHighCard = flopHighCard,
                    FlopTexture = flopTexture,
                    PfrPlayerId = pfrPlayerId,
                    DefendingPlayerId = defendingPlayerId,
                    PfrPosition = pfrPosition,
                    DefendingPosition = defendingPosition,
                    PreflopRaiseCount = preflopRaiseCount,
                    PfrInPosition = pfrInPosition,
                    FlopWentCheckCheck = flopWentCheckCheck,
                    PfrBetBb = pfrBetBb,
                    DonkBetBb = donkBetBb,
                    ResponseTo = responseTo,
                    ResponseAction = responseAction,
                    ResponseAmountBb = responseAmountBb,
                };

        return new StreetActionResult(spot, wentCheckCheck);
    }

    private static FlopTexture GetFlopTexture(PokerHandFlopDealtEvent flop)
    {
        var distinctSuitCount = new[] { flop.First.Suit, flop.Second.Suit, flop.Third.Suit }
            .Distinct()
            .Count();

        return distinctSuitCount switch
        {
            1 => FlopTexture.Monotone,
            2 => FlopTexture.TwoTone,
            3 => FlopTexture.Rainbow,
            _ => throw new InvalidOperationException("A flop must contain three cards."),
        };
    }

    private static int FindNextStreetIndex(IReadOnlyList<PokerHandEvent> events, int startIndex)
    {
        for (var index = startIndex; index < events.Count; index++)
        {
            if (events[index] is PokerHandBoardDealtEvent)
            {
                return index;
            }
        }

        return events.Count;
    }

    private static bool TryGetBetAmount(PokerHandPlayerActionEvent action, out decimal amount)
    {
        switch (action)
        {
            case PokerHandPlayerCheckEvent:
                amount = 0;
                return true;
            case PokerHandPlayerBetEvent bet:
                amount = bet.BetAmountBB;
                return true;
            default:
                amount = 0;
                return false;
        }
    }

    private static bool TryGetResponse(
        PokerHandPlayerActionEvent action,
        out PostflopResponseAction response,
        out decimal? amount
    )
    {
        switch (action)
        {
            case PokerHandPlayerFoldEvent:
                response = PostflopResponseAction.Fold;
                amount = null;
                return true;
            case PokerHandPlayerCallEvent call:
                response = PostflopResponseAction.Call;
                amount = call.CallAmountBB;
                return true;
            case PokerHandPlayerRaiseEvent raise:
                response = PostflopResponseAction.Raise;
                amount = raise.RaiseAmountBB;
                return true;
            default:
                response = default;
                amount = null;
                return false;
        }
    }

    private static int GetPostflopOrder(PokerPosition position) =>
        position switch
        {
            PokerPosition.SB => 0,
            PokerPosition.BB => 1,
            PokerPosition.LJ => 2,
            PokerPosition.HJ => 3,
            PokerPosition.CO => 4,
            PokerPosition.BTN => 5,
            _ => throw new ArgumentOutOfRangeException(nameof(position), position, null),
        };

    private sealed record StreetActionResult(PostflopBettingSpot? Spot, bool WentCheckCheck);
}
