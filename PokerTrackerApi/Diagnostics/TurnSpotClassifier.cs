using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.Diagnostics;

public static class TurnSpotClassifier
{
    public static TurnSpotClassification? Classify(PokerHand hand)
    {
        var playersAtFlop = hand.Players.Select(player => player.PlayerId).ToHashSet();
        var flopActions = new List<PokerHandPlayerActionEvent>();
        var turnActions = new List<PokerHandPlayerActionEvent>();
        var preflopRaises = 0;
        var onFlop = false;
        var onTurn = false;

        foreach (var handEvent in hand.Events.OrderBy(handEvent => handEvent.Sequence))
        {
            if (handEvent is PokerHandFlopDealtEvent)
            {
                onFlop = true;
                playersAtFlop.ExceptWith(
                    hand.Events
                        .Where(previousEvent =>
                            previousEvent.Sequence < handEvent.Sequence
                            && previousEvent is PokerHandPlayerFoldEvent
                        )
                        .Cast<PokerHandPlayerFoldEvent>()
                        .Select(fold => fold.PlayerId)
                );
                continue;
            }

            if (handEvent is PokerHandTurnDealtEvent)
            {
                onTurn = true;
                continue;
            }

            if (handEvent is PokerHandRiverDealtEvent)
            {
                break;
            }

            if (handEvent is PokerHandPlayerActionEvent playerAction)
            {
                if (!onFlop)
                {
                    if (playerAction is PokerHandPlayerRaiseEvent)
                    {
                        preflopRaises++;
                    }
                    continue;
                }

                if (!onTurn)
                {
                    flopActions.Add(playerAction);
                }
                else
                {
                    turnActions.Add(playerAction);
                }
            }
        }

        if (
            !playersAtFlop.Contains(hand.HeroPlayerId)
            || playersAtFlop.Count != 2
            || !TryGetPotType(preflopRaises, out var potType)
            || !TryClassifyFlopLine(hand.HeroPlayerId, flopActions, out var spot)
            || !TryClassifyTurnAction(
                hand.HeroPlayerId,
                spot,
                turnActions,
                out var action
            )
        )
        {
            return null;
        }

        return new TurnSpotClassification(spot, action, potType);
    }

    private static bool TryGetPotType(int preflopRaises, out TurnPotType potType)
    {
        potType = preflopRaises switch
        {
            1 => TurnPotType.SRP,
            2 => TurnPotType.ThreeBet,
            _ => default,
        };
        return preflopRaises is 1 or 2;
    }

    private static bool TryClassifyFlopLine(
        string heroPlayerId,
        IReadOnlyList<PokerHandPlayerActionEvent> actions,
        out TurnSpotType spot
    )
    {
        spot = default;
        if (
            actions.Count == 2
            && actions[0] is PokerHandPlayerCheckEvent firstCheck
            && actions[1] is PokerHandPlayerCheckEvent secondCheck
            && firstCheck.PlayerId != secondCheck.PlayerId
        )
        {
            spot =
                secondCheck.PlayerId == heroPlayerId
                    ? TurnSpotType.DelayedCBet
                    : TurnSpotType.Probe;
            return true;
        }

        if (
            actions.Count == 3
            && actions[0] is PokerHandPlayerCheckEvent oopCheck
            && actions[1] is PokerHandPlayerBetEvent heroBet
            && heroBet.PlayerId == heroPlayerId
            && actions[2] is PokerHandPlayerCallEvent oopCall
            && oopCheck.PlayerId == oopCall.PlayerId
            && oopCheck.PlayerId != heroPlayerId
        )
        {
            spot = TurnSpotType.Barrel;
            return true;
        }

        return false;
    }

    private static bool TryClassifyTurnAction(
        string heroPlayerId,
        TurnSpotType spot,
        IReadOnlyList<PokerHandPlayerActionEvent> actions,
        out TurnActionType action
    )
    {
        action = default;
        var heroActionIndex = spot == TurnSpotType.Probe ? 0 : 1;
        if (actions.Count <= heroActionIndex)
        {
            return false;
        }

        if (
            spot != TurnSpotType.Probe
            && (
                actions[0] is not PokerHandPlayerCheckEvent opponentCheck
                || opponentCheck.PlayerId == heroPlayerId
            )
        )
        {
            return false;
        }

        var heroAction = actions[heroActionIndex];
        if (heroAction.PlayerId != heroPlayerId)
        {
            return false;
        }

        if (heroAction is PokerHandPlayerCheckEvent)
        {
            action = TurnActionType.Check;
            return true;
        }

        if (heroAction is PokerHandPlayerBetEvent)
        {
            action = TurnActionType.Bet;
            return true;
        }

        return false;
    }
}

public record TurnSpotClassification(
    TurnSpotType Spot,
    TurnActionType Action,
    TurnPotType PotType
);
