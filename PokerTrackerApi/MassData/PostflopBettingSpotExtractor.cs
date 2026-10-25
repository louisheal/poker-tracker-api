using System.Diagnostics.CodeAnalysis;
using System.Text;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.MassData;

public static class PostflopBettingSpotExtractor
{
    // get the initial required hand details
    // split actions by street and filter by player action events
    // should end up with 0, 1, 2, or 3 groups of actions
    // flatmap the action groups into postflop betting spots

    // read street actions should return a list of actions
    // accepts a list of player action events
    // builds a spot after each player action
    // eg X, XB, XBF
    // eg B, BR, BRF

    private static string? CalculatePfrPlayerId(PokerHand hand) =>
        hand
            .Events.OrderBy((handEvent) => handEvent.Sequence)
            .TakeWhile((handEvent) => handEvent is not PokerHandFlopDealtEvent)
            .OfType<PokerHandPlayerActionEvent>()
            .Where((handEvent) => handEvent is PokerHandPlayerRaiseEvent or PokerHandPlayerBetEvent)
            .Select((handEvent) => handEvent.PlayerId)
            .LastOrDefault();

    private static int CalculatePreflopRaiseCount(PokerHand hand) =>
        hand
            .Events.OrderBy((handEvent) => handEvent.Sequence)
            .TakeWhile((handEvent) => handEvent is not PokerHandFlopDealtEvent)
            .Count(
                (handEvent) => handEvent is PokerHandPlayerRaiseEvent or PokerHandPlayerBetEvent
            );

    private static HashSet<string> CalculateActivePlayers(PokerHand hand) =>
        hand
            .Players.Select((player) => player.PlayerId)
            .Except(
                hand.Events.OrderBy((handEvent) => handEvent.Sequence)
                    .TakeWhile((handEvent) => handEvent is not PokerHandFlopDealtEvent)
                    .OfType<PokerHandPlayerFoldEvent>()
                    .Select((handEvent) => handEvent.PlayerId),
                StringComparer.Ordinal
            )
            .ToHashSet(StringComparer.Ordinal);

    private record FlopInfo(
        Rank FlopHighCard,
        FlopTexture FlopTexture,
        FlopRankTexture FlopRankTexture
    );

    private static FlopInfo? ExtractFlopInfo(PokerHand hand) =>
        hand
            .Events.OfType<PokerHandFlopDealtEvent>()
            .Select(
                (handEvent) =>
                    new FlopInfo(
                        GetFlopHighCard(handEvent),
                        GetFlopTexture(handEvent),
                        GetFlopRankTexture(handEvent)
                    )
            )
            .FirstOrDefault();

    private static Dictionary<PokerStreet, IReadOnlyList<PokerHandPlayerActionEvent>> PlayerActionsByStreet(PokerHand hand)
    {
        var flopActions = hand.Events
            .SkipWhile((handEvent) => handEvent is not PokerHandFlopDealtEvent)
            .TakeWhile((handEvent) => handEvent is not PokerHandTurnDealtEvent)
            .OfType<PokerHandPlayerActionEvent>()
            .ToList();
        var turnActions = hand.Events
            .SkipWhile((handEvent) => handEvent is not PokerHandTurnDealtEvent)
            .TakeWhile((handEvent) => handEvent is not PokerHandRiverDealtEvent)
            .OfType<PokerHandPlayerActionEvent>()
            .ToList();
        var riverActions = hand.Events
            .SkipWhile((handEvent) => handEvent is not PokerHandRiverDealtEvent)
            .OfType<PokerHandPlayerActionEvent>()
            .ToList();
        
        return new Dictionary<PokerStreet, IReadOnlyList<PokerHandPlayerActionEvent>>
        {
            [PokerStreet.Flop] = flopActions,
            [PokerStreet.Turn] = turnActions,
            [PokerStreet.River] = riverActions
        };
    }

    public static IReadOnlyList<PostflopBettingSpot> Extract(PokerHand hand)
    {
        var events = hand.Events.OrderBy(handEvent => handEvent.Sequence).ToArray();
        var players = hand.Players.ToDictionary(player => player.PlayerId);

        var activePlayers = CalculateActivePlayers(hand);

        string? pfrPlayerId = CalculatePfrPlayerId(hand);
        var preflopRaiseCount = CalculatePreflopRaiseCount(hand);

        var flopInfo = ExtractFlopInfo(hand);
        if (flopInfo is null || activePlayers.Count != 2 || pfrPlayerId is null)
        {
            return [];
        }

        var defendingPlayerId = activePlayers.Single(playerId => playerId != pfrPlayerId);
        var pfr = players[pfrPlayerId];
        var defender = players[defendingPlayerId];
        var pfrInPosition = GetPostflopOrder(pfr.Position) > GetPostflopOrder(defender.Position);

        var actionsByStreet = PlayerActionsByStreet(hand);

        // TODO : we should have a dynamic list of spots
        var spots = new List<PostflopBettingSpot>(3);

        // TODO : do we really need FlopIndex?
        // What if we extracted the player actions per street
        var flopEnd = FindNextStreetIndex(events, flopInfo.FlopIndex + 1);
        var flopActionSequence = GetActionSequence(events, flopInfo.FlopIndex + 1, flopEnd);

        var flopActions = ReadStreetActions(
            actionsByStreet[PokerStreet.Flop],
            hand.HandId,
            hand.HeroPlayerId,
            PokerStreet.Flop,
            pfrPlayerId,
            defendingPlayerId,
            pfr.Position,
            defender.Position,
            preflopRaiseCount,
            pfrInPosition,
            flopInfo.FlopHighCard,
            flopInfo.FlopTexture,
            new PostflopBettingContext(
                FlopActionSequence: flopActionSequence,
                FlopRankTexture: flopInfo.FlopRankTexture
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
            var turnRunout = GetTurnRunout(flopInfo.FlopEvent, turnDealt.Card);
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
                flopInfo.FlopHighCard,
                flopInfo.FlopTexture,
                new PostflopBettingContext(
                    FlopActionSequence: flopActionSequence,
                    FlopRankTexture: flopInfo.FlopRankTexture,
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
                    flopInfo.FlopHighCard,
                    flopInfo.FlopTexture,
                    new PostflopBettingContext(
                        FlopActionSequence: flopActionSequence,
                        FlopRankTexture: flopInfo.FlopRankTexture,
                        TurnActionSequence: turnActionSequence,
                        TurnRunout: turnRunout,
                        // TODO : this only needs the flop event for the community cards
                        RiverRunout: GetRiverRunout(
                            flopInfo.FlopEvent,
                            turnDealt.Card,
                            riverDealt.Card
                        )
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

    // TODO : why does ReadStreetActions return a single result?
    // XBRF line is XBR and XBRF
    private static StreetActionResult ReadStreetActions(
        // TODO : can change type to PokerHandPlayerActionEvent
        IReadOnlyList<PokerHandEvent> handEvents,
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

        foreach (var handEvent in handEvents)
        {
            // TODO : can be removed - we only pass in action events now
            if (handEvent is not PokerHandPlayerActionEvent action)
            {
                continue;
            }

            // TODO : might be an unnecessary guard
            if (action.PlayerId != pfrPlayerId && action.PlayerId != defendingPlayerId)
            {
                continue;
            }

            // TODO : yield return and break?
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
                    // TODO : really poor
                    // recalcuating the pot size every time
                    pendingBetToPotRatio = GetBetToPotRatio(handEvents, index, bet.BetAmountBB);
                    sawBet = true;
                }
            }
            else
            {
                defenderActed = true;
                if (!sawBet && action is PokerHandPlayerBetEvent bet)
                {
                    pendingBettorId = defendingPlayerId;
                    pendingBetToPotRatio = GetBetToPotRatio(handEvents, index, bet.BetAmountBB);
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

    private static Rank GetFlopHighCard(PokerHandFlopDealtEvent flop) =>
        new[] { flop.First.Rank, flop.Second.Rank, flop.Third.Rank }.Max();

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
        [NotNullWhen(true)] out PostflopResponseAction? response
    )
    {
        switch (action)
        {
            case PokerHandPlayerFoldEvent:
                response = PostflopResponseAction.Fold;
                return true;
            case PokerHandPlayerCallEvent:
                response = PostflopResponseAction.Call;
                return true;
            case PokerHandPlayerRaiseEvent:
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
