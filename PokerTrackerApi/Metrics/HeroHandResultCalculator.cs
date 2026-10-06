using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.Metrics;

public static class HeroHandResultCalculator
{
    public static HeroHandResult Calculate(PokerHand hand)
    {
        var investedBB = 0m;
        var wonBB = 0m;
        var heroStreetBet = 0m;
        var heroFolded = false;
        var sawFlop = false;
        var wentToShowdown = false;

        foreach (var handEvent in hand.Events.OrderBy(handEvent => handEvent.Sequence))
        {
            switch (handEvent)
            {
                case PokerHandBoardDealtEvent boardDealt:
                    heroStreetBet = 0m;
                    if (boardDealt is PokerHandFlopDealtEvent && !heroFolded)
                    {
                        sawFlop = true;
                    }
                    break;
                case PokerHandAntePostEvent ante when ante.PlayerId == hand.HeroPlayerId:
                    investedBB += ante.AmountBB;
                    break;
                case PokerHandSmallBlindPostEvent smallBlind
                    when smallBlind.PlayerId == hand.HeroPlayerId:
                    investedBB += smallBlind.AmountBB;
                    heroStreetBet += smallBlind.AmountBB;
                    break;
                case PokerHandBigBlindPostEvent bigBlind
                    when bigBlind.PlayerId == hand.HeroPlayerId:
                    investedBB += bigBlind.AmountBB;
                    heroStreetBet += bigBlind.AmountBB;
                    break;
                case PokerHandPlayerCallEvent call when call.PlayerId == hand.HeroPlayerId:
                    investedBB += call.CallAmountBB;
                    heroStreetBet += call.CallAmountBB;
                    break;
                case PokerHandPlayerBetEvent bet when bet.PlayerId == hand.HeroPlayerId:
                    investedBB += bet.BetAmountBB;
                    heroStreetBet += bet.BetAmountBB;
                    break;
                case PokerHandPlayerRaiseEvent raise when raise.PlayerId == hand.HeroPlayerId:
                    var raiseContribution = raise.RaiseToAmountBB - heroStreetBet;
                    investedBB += raiseContribution;
                    heroStreetBet += raiseContribution;
                    break;
                case PokerHandUncalledBetReturnedEvent returned
                    when returned.PlayerId == hand.HeroPlayerId:
                    var amountReturned = Math.Min(returned.AmountBB, heroStreetBet);
                    investedBB -= amountReturned;
                    heroStreetBet -= amountReturned;
                    break;
                case PokerHandPotAwardedEvent award when award.PlayerId == hand.HeroPlayerId:
                    wonBB += award.AmountBB;
                    break;
                case PokerHandPlayerFoldEvent fold when fold.PlayerId == hand.HeroPlayerId:
                    if (!wentToShowdown)
                    {
                        heroFolded = true;
                    }
                    break;
                case PokerHandCardsShownEvent:
                    if (!heroFolded)
                    {
                        wentToShowdown = true;
                    }
                    break;
            }
        }

        return new HeroHandResult(
            wonBB - investedBB,
            sawFlop,
            wentToShowdown,
            wentToShowdown && wonBB > 0m
        );
    }
}
