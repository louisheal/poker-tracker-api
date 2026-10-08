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
                var riverShowdownOutcome = GetRiverShowdownOutcome(
                    events,
                    turnEnd + 1,
                    riverEnd,
                    hand.HeroPlayerId,
                    pfrPlayerId,
                    defendingPlayerId,
                    out var riverWentToShowdown
                );
                var riverActionContext = GetRiverActionContext(
                    events,
                    turnEnd + 1,
                    riverEnd,
                    hand.HeroPlayerId,
                    pfrPlayerId,
                    defendingPlayerId,
                    riverWentToShowdown
                );
                var riverActions = ReadStreetActions(
                    events,
                    turnEnd + 1,
                    riverEnd,
                    hand.HandId,
                    hand.HeroPlayerId,
                    hand.Timestamp,
                    PokerStreet.River,
                    pfrPlayerId,
                    defendingPlayerId,
                    pfr.Position,
                    defender.Position,
                    preflopRaiseCount,
                    pfrInPosition,
                    flopActions.WentCheckCheck,
                    flopHighCard.Value,
                    flopTexture.Value,
                    new PostflopBettingContext(
                        FlopActionSequence: flopActionSequence,
                        FlopRankTexture: flopRankTexture,
                        TurnActionSequence: turnActionSequence,
                        TurnRunout: turnRunout,
                        RiverRunout: GetRiverRunout(flopEvent, turnDealt.Card, riverDealt.Card),
                        VillainRiverBet: riverActionContext.VillainBet,
                        VillainRiverRaise: riverActionContext.VillainRaise,
                        VillainRiverBetShowdownOutcome: riverActionContext.BetShowdownOutcome,
                        VillainRiverRaiseShowdownOutcome: riverActionContext.RaiseShowdownOutcome,
                        HeroResponseToVillainRiverBet: riverActionContext.HeroResponseToVillainBet,
                        HeroResponseToVillainRiverRaise: riverActionContext.HeroResponseToVillainRaise,
                        VillainResponseToHeroRiverBet: riverActionContext.VillainResponseToHeroBet,
                        HeroRiverBetToPotRatio: riverActionContext.HeroRiverBetToPotRatio,
                        RiverWentToShowdown: riverWentToShowdown,
                        RiverShowdownOutcome: riverShowdownOutcome
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
        FlopTexture flopTexture,
        PostflopBettingContext? context = null
    )
    {
        decimal? pfrBetBb = null;
        decimal? donkBetBb = null;
        PostflopResponseTo? responseTo = null;
        PostflopResponseAction? responseAction = null;
        decimal? responseAmountBb = null;
        string? pendingBettorId = null;
        decimal? pendingBetToPotRatio = null;
        RiverBetResponseLine? riverBetResponseLine = null;
        decimal? riverBetToPotRatio = null;
        var pfrActed = false;
        var defenderActed = false;
        var checkedPlayerIds = new HashSet<string>(StringComparer.Ordinal);
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
            if (action is PokerHandPlayerCheckEvent)
            {
                checkedPlayerIds.Add(action.PlayerId);
            }

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
                    if (street == PokerStreet.River)
                    {
                        riverBetResponseLine = checkedPlayerIds.Contains(action.PlayerId)
                            ? RiverBetResponseLine.XBF
                            : RiverBetResponseLine.BF;
                        riverBetToPotRatio = pendingBetToPotRatio;
                    }
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
                if (!sawBet && TryGetBetAmount(action, out var betAmount))
                {
                    pfrBetBb = betAmount;
                    if (betAmount > 0)
                    {
                        pendingBettorId = pfrPlayerId;
                        pendingBetToPotRatio =
                            street == PokerStreet.River
                                ? GetBetToPotRatio(events, index, betAmount)
                                : null;
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
                        pendingBetToPotRatio =
                            street == PokerStreet.River
                                ? GetBetToPotRatio(events, index, betAmount)
                                : null;
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
                    FlopWentCheckCheck = flopWentCheckCheck,
                    PfrBetBb = pfrBetBb,
                    DonkBetBb = donkBetBb,
                    ResponseTo = responseTo,
                    ResponseAction = responseAction,
                    ResponseAmountBb = responseAmountBb,
                    RiverBetResponseLine = riverBetResponseLine,
                    RiverBetToPotRatio = riverBetToPotRatio,
                    VillainRiverBet = context?.VillainRiverBet ?? false,
                    VillainRiverRaise = context?.VillainRiverRaise ?? false,
                    VillainRiverBetShowdownOutcome = context?.VillainRiverBetShowdownOutcome,
                    VillainRiverRaiseShowdownOutcome = context?.VillainRiverRaiseShowdownOutcome,
                    HeroCalledVillainRiverBet =
                        context?.HeroResponseToVillainRiverBet == PostflopResponseAction.Call,
                    HeroCalledVillainRiverRaise =
                        context?.HeroResponseToVillainRiverRaise == PostflopResponseAction.Call,
                    HeroResponseToVillainRiverBet = context?.HeroResponseToVillainRiverBet,
                    HeroResponseToVillainRiverRaise = context?.HeroResponseToVillainRiverRaise,
                    VillainResponseToHeroRiverBet = context?.VillainResponseToHeroRiverBet,
                    HeroRiverBetToPotRatio = context?.HeroRiverBetToPotRatio,
                    RiverWentToShowdown = context?.RiverWentToShowdown ?? false,
                    RiverShowdownOutcome = context?.RiverShowdownOutcome,
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

    private static RiverActionContext GetRiverActionContext(
        IReadOnlyList<PokerHandEvent> events,
        int startIndex,
        int endIndex,
        string heroPlayerId,
        string pfrPlayerId,
        string defendingPlayerId,
        bool riverWentToShowdown
    )
    {
        var heroIsInHand = heroPlayerId == pfrPlayerId || heroPlayerId == defendingPlayerId;
        var villainPlayerId =
            heroPlayerId == pfrPlayerId ? defendingPlayerId
            : heroPlayerId == defendingPlayerId ? pfrPlayerId
            : null;
        var riverActions = events.Skip(startIndex).Take(endIndex - startIndex).ToArray();
        var villainBet = riverActions
            .OfType<PokerHandPlayerBetEvent>()
            .FirstOrDefault(action => !heroIsInHand || action.PlayerId != heroPlayerId);
        var villainRaise = riverActions
            .OfType<PokerHandPlayerRaiseEvent>()
            .FirstOrDefault(action => !heroIsInHand || action.PlayerId != heroPlayerId);
        var heroBetIndex = Enumerable
            .Range(startIndex, endIndex - startIndex)
            .FirstOrDefault(
                index =>
                    events[index] is PokerHandPlayerBetEvent bet && bet.PlayerId == heroPlayerId,
                -1
            );
        decimal? heroBetToPotRatio =
            heroBetIndex < 0 ? null
            : GetPotBeforeEvent(events, heroBetIndex) is var potBeforeBet && potBeforeBet > 0
                ? ((PokerHandPlayerBetEvent)events[heroBetIndex]).BetAmountBB / potBeforeBet
            : null;

        var villainBetOpponentId =
            villainBet?.PlayerId == pfrPlayerId ? defendingPlayerId
            : villainBet?.PlayerId == defendingPlayerId ? pfrPlayerId
            : null;
        var villainRaiseOpponentId =
            villainRaise?.PlayerId == pfrPlayerId ? defendingPlayerId
            : villainRaise?.PlayerId == defendingPlayerId ? pfrPlayerId
            : null;
        if (villainPlayerId is null)
        {
            return new RiverActionContext(
                villainBet is not null,
                villainRaise is not null,
                null,
                null,
                null,
                heroBetToPotRatio,
                GetVillainRiverShowdownOutcome(
                    events,
                    villainBet?.PlayerId,
                    villainBetOpponentId,
                    riverWentToShowdown
                ),
                GetVillainRiverShowdownOutcome(
                    events,
                    villainRaise?.PlayerId,
                    villainRaiseOpponentId,
                    riverWentToShowdown
                )
            );
        }

        return new RiverActionContext(
            villainBet is not null,
            villainRaise is not null,
            GetDirectResponseToAction(
                events,
                startIndex,
                endIndex,
                villainPlayerId,
                heroPlayerId,
                aggressorActionIsRaise: false
            ),
            GetDirectResponseToAction(
                events,
                startIndex,
                endIndex,
                villainPlayerId,
                heroPlayerId,
                aggressorActionIsRaise: true
            ),
            GetDirectResponseToAction(
                events,
                startIndex,
                endIndex,
                heroPlayerId,
                villainPlayerId,
                aggressorActionIsRaise: false
            ),
            heroBetToPotRatio,
            GetVillainRiverShowdownOutcome(
                events,
                villainBet?.PlayerId,
                villainBetOpponentId,
                riverWentToShowdown
            ),
            GetVillainRiverShowdownOutcome(
                events,
                villainRaise?.PlayerId,
                villainRaiseOpponentId,
                riverWentToShowdown
            )
        );
    }

    private static VillainRiverShowdownOutcome? GetVillainRiverShowdownOutcome(
        IReadOnlyList<PokerHandEvent> events,
        string? aggressorPlayerId,
        string? opponentPlayerId,
        bool riverWentToShowdown
    )
    {
        if (!riverWentToShowdown || aggressorPlayerId is null || opponentPlayerId is null)
        {
            return null;
        }

        var awards = events
            .OfType<PokerHandPotAwardedEvent>()
            .Where(award =>
                award.PlayerId == aggressorPlayerId || award.PlayerId == opponentPlayerId
            )
            .GroupBy(award => award.PlayerId)
            .ToDictionary(group => group.Key, group => group.Sum(award => award.AmountBB));
        var aggressorAward = awards.GetValueOrDefault(aggressorPlayerId);
        var opponentAward = awards.GetValueOrDefault(opponentPlayerId);

        if (aggressorAward > 0 && opponentAward > 0)
        {
            return VillainRiverShowdownOutcome.Chop;
        }

        if (aggressorAward > 0)
        {
            return VillainRiverShowdownOutcome.Win;
        }

        return opponentAward > 0 ? VillainRiverShowdownOutcome.Loss : null;
    }

    private static PostflopResponseAction? GetDirectResponseToAction(
        IReadOnlyList<PokerHandEvent> events,
        int startIndex,
        int endIndex,
        string aggressorPlayerId,
        string responderPlayerId,
        bool aggressorActionIsRaise
    )
    {
        for (var index = startIndex; index < endIndex; index++)
        {
            var isAggression = aggressorActionIsRaise
                ? events[index] is PokerHandPlayerRaiseEvent raise
                    && raise.PlayerId == aggressorPlayerId
                : events[index] is PokerHandPlayerBetEvent bet && bet.PlayerId == aggressorPlayerId;
            if (!isAggression)
            {
                continue;
            }

            for (var responseIndex = index + 1; responseIndex < endIndex; responseIndex++)
            {
                if (
                    events[responseIndex] is PokerHandPlayerActionEvent response
                    && response.PlayerId == responderPlayerId
                )
                {
                    return TryGetResponse(response, out var responseAction, out _)
                        ? responseAction
                        : null;
                }
            }

            return null;
        }

        return null;
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

    private static RiverShowdownOutcome? GetRiverShowdownOutcome(
        IReadOnlyList<PokerHandEvent> events,
        int startIndex,
        int endIndex,
        string heroPlayerId,
        string pfrPlayerId,
        string defendingPlayerId,
        out bool riverWentToShowdown
    )
    {
        riverWentToShowdown = events
            .Skip(startIndex)
            .Take(endIndex - startIndex)
            .OfType<PokerHandCardsShownEvent>()
            .Any();
        if (!riverWentToShowdown)
        {
            return null;
        }

        var awards = events
            .OfType<PokerHandPotAwardedEvent>()
            .Where(award =>
                award.PlayerId == heroPlayerId
                || award.PlayerId == pfrPlayerId
                || award.PlayerId == defendingPlayerId
            )
            .GroupBy(award => award.PlayerId)
            .ToDictionary(group => group.Key, group => group.Sum(award => award.AmountBB));
        var heroAward = awards.GetValueOrDefault(heroPlayerId);
        var villainPlayerId = heroPlayerId == pfrPlayerId ? defendingPlayerId : pfrPlayerId;
        var villainAward = awards.GetValueOrDefault(villainPlayerId);

        if (heroAward > 0 && villainAward > 0)
        {
            return RiverShowdownOutcome.Chop;
        }

        if (heroAward > 0)
        {
            return RiverShowdownOutcome.HeroWin;
        }

        return villainAward > 0 ? RiverShowdownOutcome.VillainWin : null;
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

    private sealed record PostflopBettingContext(
        PostflopActionSequence? FlopActionSequence = null,
        FlopRankTexture? FlopRankTexture = null,
        PostflopActionSequence? TurnActionSequence = null,
        PostflopRunout? TurnRunout = null,
        PostflopRunout? RiverRunout = null,
        bool VillainRiverBet = false,
        bool VillainRiverRaise = false,
        VillainRiverShowdownOutcome? VillainRiverBetShowdownOutcome = null,
        VillainRiverShowdownOutcome? VillainRiverRaiseShowdownOutcome = null,
        PostflopResponseAction? HeroResponseToVillainRiverBet = null,
        PostflopResponseAction? HeroResponseToVillainRiverRaise = null,
        PostflopResponseAction? VillainResponseToHeroRiverBet = null,
        decimal? HeroRiverBetToPotRatio = null,
        bool RiverWentToShowdown = false,
        RiverShowdownOutcome? RiverShowdownOutcome = null
    );

    private sealed record RiverActionContext(
        bool VillainBet,
        bool VillainRaise,
        PostflopResponseAction? HeroResponseToVillainBet,
        PostflopResponseAction? HeroResponseToVillainRaise,
        PostflopResponseAction? VillainResponseToHeroBet,
        decimal? HeroRiverBetToPotRatio,
        VillainRiverShowdownOutcome? BetShowdownOutcome,
        VillainRiverShowdownOutcome? RaiseShowdownOutcome
    );
}
