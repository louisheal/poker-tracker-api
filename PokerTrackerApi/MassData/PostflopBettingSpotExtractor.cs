using System.Text;
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
        PokerHandFlopDealtEvent? flopEvent = null;
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
                    flopEvent = flopDealt;
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
            || flopEvent is null
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
        var spots = new List<PostflopBettingSpot>(3);
        var flopEnd = FindNextStreetIndex(events, flopIndex + 1);
        var flopActionSequence = GetActionSequence(events, flopIndex + 1, flopEnd);
        var flopRankTexture = GetFlopRankTexture(flopEvent);
        var flopActions = ReadStreetActions(
            events,
            flopIndex + 1,
            flopEnd,
            hand.HandId,
            hand.HeroPlayerId,
            PokerStreet.Flop,
            pfrPlayerId,
            defendingPlayerId,
            pfr.Position,
            defender.Position,
            preflopRaiseCount,
            pfrInPosition,
            flopHighCard.Value,
            flopTexture.Value,
            new PostflopBettingContext(
                FlopActionSequence: flopActionSequence,
                FlopRankTexture: flopRankTexture
            )
        );

        if (flopActions.Spot is not null)
        {
            spots.Add(flopActions.Spot);
        }

        if (flopEnd < events.Length && events[flopEnd] is PokerHandTurnDealtEvent)
        {
            var turnEnd = FindNextStreetIndex(events, flopEnd + 1);
            var turnDealt = (PokerHandTurnDealtEvent)events[flopEnd];
            var turnActionSequence = GetActionSequence(events, flopEnd + 1, turnEnd);
            var turnRunout = GetTurnRunout(flopEvent, turnDealt.Card);
            var turnActions = ReadStreetActions(
                events,
                flopEnd + 1,
                turnEnd,
                hand.HandId,
                hand.HeroPlayerId,
                PokerStreet.Turn,
                pfrPlayerId,
                defendingPlayerId,
                pfr.Position,
                defender.Position,
                preflopRaiseCount,
                pfrInPosition,
                flopHighCard.Value,
                flopTexture.Value,
                new PostflopBettingContext(
                    FlopActionSequence: flopActionSequence,
                    FlopRankTexture: flopRankTexture,
                    TurnActionSequence: turnActionSequence,
                    TurnRunout: turnRunout
                )
            );

            if (turnActions.Spot is not null)
            {
                spots.Add(turnActions.Spot);
            }

            if (turnEnd < events.Length && events[turnEnd] is PokerHandRiverDealtEvent riverDealt)
            {
                var riverEnd = FindNextStreetIndex(events, turnEnd + 1);
                var riverActions = ReadStreetActions(
                    events,
                    turnEnd + 1,
                    riverEnd,
                    hand.HandId,
                    hand.HeroPlayerId,
                    PokerStreet.River,
                    pfrPlayerId,
                    defendingPlayerId,
                    pfr.Position,
                    defender.Position,
                    preflopRaiseCount,
                    pfrInPosition,
                    flopHighCard.Value,
                    flopTexture.Value,
                    new PostflopBettingContext(
                        FlopActionSequence: flopActionSequence,
                        FlopRankTexture: flopRankTexture,
                        TurnActionSequence: turnActionSequence,
                        TurnRunout: turnRunout,
                        RiverRunout: GetRiverRunout(flopEvent, turnDealt.Card, riverDealt.Card)
                    )
                );

                if (riverActions.Spot is not null)
                {
                    spots.Add(riverActions.Spot);
                }
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
        PokerStreet street,
        string pfrPlayerId,
        string defendingPlayerId,
        PokerPosition pfrPosition,
        PokerPosition defendingPosition,
        int preflopRaiseCount,
        bool pfrInPosition,
        Rank flopHighCard,
        FlopTexture flopTexture,
        PostflopBettingContext? context = null
    )
    {
        PostflopResponseTo? responseTo = null;
        PostflopResponseAction? responseAction = null;
        string? pendingBettorId = null;
        decimal? pendingBetToPotRatio = null;
        PostflopBetResponseLine? betResponseLine = null;
        decimal? betToPotRatio = null;
        var pfrActed = false;
        var defenderActed = false;
        var checkedPlayerIds = new HashSet<string>(StringComparer.Ordinal);
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

            if (action is PokerHandPlayerCheckEvent)
            {
                checkedPlayerIds.Add(action.PlayerId);
            }

            if (pendingBettorId is not null && action.PlayerId != pendingBettorId)
            {
                if (TryGetResponse(action, out var currentResponse))
                {
                    responseTo =
                        pendingBettorId == pfrPlayerId
                            ? PostflopResponseTo.PfrBet
                            : PostflopResponseTo.DonkBet;
                    responseAction = currentResponse;
                    betResponseLine = checkedPlayerIds.Contains(action.PlayerId)
                        ? PostflopBetResponseLine.XBF
                        : PostflopBetResponseLine.BF;
                    betToPotRatio = pendingBetToPotRatio;
                    pendingBettorId = null;
                    pendingBetToPotRatio = null;
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
                if (!sawBet && action is PokerHandPlayerBetEvent bet)
                {
                    pendingBettorId = pfrPlayerId;
                    pendingBetToPotRatio = GetBetToPotRatio(events, index, bet.BetAmountBB);
                    sawBet = true;
                }
            }
            else
            {
                defenderActed = true;
                if (!sawBet && action is PokerHandPlayerBetEvent bet)
                {
                    pendingBettorId = defendingPlayerId;
                    pendingBetToPotRatio = GetBetToPotRatio(events, index, bet.BetAmountBB);
                    sawBet = true;
                }
            }
        }

        var spot = !sawBet
            ? null
            : new PostflopBettingSpot
            {
                HandId = handId,
                HeroPlayerId = heroPlayerId,
                Street = street,
                FlopHighCard = flopHighCard,
                FlopTexture = flopTexture,
                FlopActionSequence = context?.FlopActionSequence,
                FlopRankTexture = context?.FlopRankTexture,
                TurnActionSequence = context?.TurnActionSequence,
                TurnRunout = context?.TurnRunout,
                RiverRunout = context?.RiverRunout,
                PfrPlayerId = pfrPlayerId,
                DefendingPlayerId = defendingPlayerId,
                PfrPosition = pfrPosition,
                DefendingPosition = defendingPosition,
                PreflopRaiseCount = preflopRaiseCount,
                PfrInPosition = pfrInPosition,
                ResponseTo = responseTo,
                ResponseAction = responseAction,
                BetResponseLine = betResponseLine,
                BetToPotRatio = betToPotRatio,
            };

        return new StreetActionResult(spot);
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

    private static FlopRankTexture GetFlopRankTexture(PokerHandFlopDealtEvent flop) =>
        new[] { flop.First.Rank, flop.Second.Rank, flop.Third.Rank }.Distinct().Count() switch
        {
            1 => FlopRankTexture.Trips,
            2 => FlopRankTexture.Paired,
            3 => FlopRankTexture.Unpaired,
            _ => throw new InvalidOperationException("A flop must contain three cards."),
        };

    private static PostflopActionSequence? GetActionSequence(
        IReadOnlyList<PokerHandEvent> events,
        int startIndex,
        int endIndex
    )
    {
        var sequence = new StringBuilder();

        for (var index = startIndex; index < endIndex; index++)
        {
            switch (events[index])
            {
                case PokerHandPlayerCheckEvent:
                    sequence.Append('X');
                    break;
                case PokerHandPlayerBetEvent:
                    sequence.Append('B');
                    break;
                case PokerHandPlayerRaiseEvent:
                    sequence.Append('R');
                    break;
                case PokerHandPlayerCallEvent:
                    sequence.Append('C');
                    break;
            }
        }

        PostflopActionSequence? actionSequence = sequence.ToString() switch
        {
            "XX" => PostflopActionSequence.XX,
            "XBC" => PostflopActionSequence.XBC,
            "XBRC" => PostflopActionSequence.XBRC,
            "BC" => PostflopActionSequence.BC,
            _ => null,
        };

        return actionSequence;
    }

    private static PostflopRunout GetTurnRunout(PokerHandFlopDealtEvent flop, PlayingCard turnCard)
    {
        var flopCards = new[] { flop.First, flop.Second, flop.Third };
        if (flopCards.All(card => turnCard.Rank > card.Rank))
        {
            return PostflopRunout.Overcard;
        }

        if (flopCards.Any(card => turnCard.Rank == card.Rank))
        {
            return PostflopRunout.Paired;
        }

        if (flopCards.Count(card => turnCard.Suit == card.Suit) == 2)
        {
            return PostflopRunout.FlushCompleting;
        }

        return PostflopRunout.Other;
    }

    private static PostflopRunout GetRiverRunout(
        PokerHandFlopDealtEvent flop,
        PlayingCard turnCard,
        PlayingCard riverCard
    )
    {
        var boardCards = new[] { flop.First, flop.Second, flop.Third, turnCard };
        if (boardCards.All(card => riverCard.Rank > card.Rank))
        {
            return PostflopRunout.Overcard;
        }

        if (boardCards.Any(card => riverCard.Rank == card.Rank))
        {
            return PostflopRunout.Paired;
        }

        if (boardCards.Count(card => riverCard.Suit == card.Suit) >= 2)
        {
            return PostflopRunout.FlushCompleting;
        }

        return PostflopRunout.Other;
    }

    private static decimal GetPotBeforeEvent(IReadOnlyList<PokerHandEvent> events, int eventIndex)
    {
        var potBb = 0m;
        var streetBets = new Dictionary<string, decimal>(StringComparer.Ordinal);

        for (var index = 0; index < eventIndex; index++)
        {
            switch (events[index])
            {
                case PokerHandBoardDealtEvent:
                    streetBets.Clear();
                    break;
                case PokerHandPostEvent post:
                    potBb += post.AmountBB;
                    if (post.PostType != PostType.Ante)
                    {
                        AddStreetContribution(streetBets, post.PlayerId, post.AmountBB);
                    }
                    break;
                case PokerHandPlayerBetEvent bet:
                    potBb += bet.BetAmountBB;
                    AddStreetContribution(streetBets, bet.PlayerId, bet.BetAmountBB);
                    break;
                case PokerHandPlayerCallEvent call:
                    potBb += call.CallAmountBB;
                    AddStreetContribution(streetBets, call.PlayerId, call.CallAmountBB);
                    break;
                case PokerHandPlayerRaiseEvent raise:
                    var raiseContribution = Math.Max(
                        0m,
                        raise.RaiseToAmountBB - GetStreetContribution(streetBets, raise.PlayerId)
                    );
                    potBb += raiseContribution;
                    AddStreetContribution(streetBets, raise.PlayerId, raiseContribution);
                    break;
                case PokerHandUncalledBetReturnedEvent returned:
                    var amountReturned = Math.Min(
                        returned.AmountBB,
                        GetStreetContribution(streetBets, returned.PlayerId)
                    );
                    potBb = Math.Max(0m, potBb - amountReturned);
                    AddStreetContribution(streetBets, returned.PlayerId, -amountReturned);
                    break;
                case PokerHandCashDropEvent cashDrop:
                    potBb = Math.Max(0m, potBb - cashDrop.AmountBB);
                    break;
            }
        }

        return potBb;
    }

    private static decimal? GetBetToPotRatio(
        IReadOnlyList<PokerHandEvent> events,
        int betEventIndex,
        decimal betAmountBb
    )
    {
        var potBeforeBet = GetPotBeforeEvent(events, betEventIndex);
        return potBeforeBet > 0 ? betAmountBb / potBeforeBet : null;
    }

    private static void AddStreetContribution(
        IDictionary<string, decimal> streetBets,
        string playerId,
        decimal amountBb
    )
    {
        streetBets[playerId] = GetStreetContribution(streetBets, playerId) + amountBb;
    }

    private static decimal GetStreetContribution(
        IDictionary<string, decimal> streetBets,
        string playerId
    ) => streetBets.TryGetValue(playerId, out var contribution) ? contribution : 0m;

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

    private static bool TryGetResponse(
        PokerHandPlayerActionEvent action,
        out PostflopResponseAction response
    )
    {
        switch (action)
        {
            case PokerHandPlayerFoldEvent:
                response = PostflopResponseAction.Fold;
                return true;
            case PokerHandPlayerCallEvent call:
                response = PostflopResponseAction.Call;
                return true;
            case PokerHandPlayerRaiseEvent raise:
                response = PostflopResponseAction.Raise;
                return true;
            default:
                response = default;
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

    private sealed record StreetActionResult(PostflopBettingSpot? Spot);

    private sealed record PostflopBettingContext(
        PostflopActionSequence? FlopActionSequence = null,
        FlopRankTexture? FlopRankTexture = null,
        PostflopActionSequence? TurnActionSequence = null,
        PostflopRunout? TurnRunout = null,
        PostflopRunout? RiverRunout = null
    );
}
