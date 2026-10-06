using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.Diagnostics;

public static class RiverSpotClassifier
{
    public static RiverSpotClassification? Classify(PokerHand hand)
    {
        var onRiver = false;
        var potBB = 0m;
        var streetBets = new Dictionary<string, decimal>();
        RiverSpotClassification? classification = null;
        RiverBetSizeCategory? lastRiverAggressionSize = null;

        foreach (var handEvent in hand.Events.OrderBy(handEvent => handEvent.Sequence))
        {
            if (handEvent is PokerHandBoardDealtEvent)
            {
                streetBets.Clear();
                onRiver = handEvent is PokerHandRiverDealtEvent;
                continue;
            }

            switch (handEvent)
            {
                case PokerHandPostEvent post:
                    potBB += post.AmountBB;
                    if (post.PostType != PostType.Ante)
                    {
                        AddStreetBet(streetBets, post.PlayerId, post.AmountBB);
                    }
                    break;
                case PokerHandPlayerBetEvent bet:
                    if (onRiver)
                    {
                        var size = ClassifySize(bet.BetAmountBB, potBB);
                        if (bet.PlayerId == hand.HeroPlayerId)
                        {
                            classification = new RiverSpotClassification(
                                RiverSpotType.HeroBetRiver,
                                size
                            );
                        }
                        lastRiverAggressionSize = size;
                    }
                    potBB += bet.BetAmountBB;
                    AddStreetBet(streetBets, bet.PlayerId, bet.BetAmountBB);
                    break;
                case PokerHandPlayerRaiseEvent raise:
                    var raiseContribution =
                        raise.RaiseToAmountBB - streetBets.GetValueOrDefault(raise.PlayerId);
                    if (onRiver)
                    {
                        var size = ClassifySize(raiseContribution, potBB);
                        if (raise.PlayerId == hand.HeroPlayerId)
                        {
                            classification = new RiverSpotClassification(
                                RiverSpotType.HeroRaiseRiver,
                                size
                            );
                        }
                        lastRiverAggressionSize = size;
                    }
                    potBB += raiseContribution;
                    AddStreetBet(streetBets, raise.PlayerId, raiseContribution);
                    break;
                case PokerHandPlayerCallEvent call:
                    if (onRiver && call.PlayerId == hand.HeroPlayerId)
                    {
                        classification = new RiverSpotClassification(
                            RiverSpotType.HeroCallRiver,
                            lastRiverAggressionSize ?? ClassifySize(call.CallAmountBB, potBB)
                        );
                    }
                    potBB += call.CallAmountBB;
                    AddStreetBet(streetBets, call.PlayerId, call.CallAmountBB);
                    break;
                case PokerHandPlayerCheckEvent check
                    when onRiver && check.PlayerId == hand.HeroPlayerId:
                    classification = new RiverSpotClassification(
                        RiverSpotType.HeroCheckRiver,
                        null
                    );
                    break;
                case PokerHandPlayerFoldEvent fold
                    when onRiver && fold.PlayerId == hand.HeroPlayerId:
                    classification = null;
                    break;
                case PokerHandUncalledBetReturnedEvent returned:
                    var amountReturned = Math.Min(
                        returned.AmountBB,
                        streetBets.GetValueOrDefault(returned.PlayerId)
                    );
                    potBB -= amountReturned;
                    AddStreetBet(streetBets, returned.PlayerId, -amountReturned);
                    break;
            }
        }

        return classification;
    }

    private static void AddStreetBet(
        IDictionary<string, decimal> streetBets,
        string playerId,
        decimal amountBB
    )
    {
        streetBets[playerId] = streetBets.TryGetValue(playerId, out var currentBet)
            ? currentBet + amountBB
            : amountBB;
    }

    private static RiverBetSizeCategory? ClassifySize(decimal amountBB, decimal potBeforeBB)
    {
        if (amountBB <= 0m || potBeforeBB <= 0m)
        {
            return null;
        }

        var potFraction = amountBB / potBeforeBB;
        if (potFraction < 0.4m)
        {
            return RiverBetSizeCategory.Small;
        }
        if (potFraction < 0.7m)
        {
            return RiverBetSizeCategory.Medium;
        }
        if (potFraction < 1m)
        {
            return RiverBetSizeCategory.Large;
        }

        return RiverBetSizeCategory.Overbet;
    }
}

public record RiverSpotClassification(RiverSpotType Spot, RiverBetSizeCategory? SizeCategory);
