using PokerTrackerApi.Domain.HandHistories;
using PokerTrackerApi.Domain.HandHistories.Events;

namespace PokerTrackerApi.Winrate;

public static class WinrateCalculator
{
    public static Winrate CalulcateWinrate(HandHistory hand)
    {
        var state = new WinrateState();
        foreach (var evnt in hand.Events)
        {
            state.Apply(evnt, hand.HeroPlayerId);
        }

        return state.GetWinrate();
    }
}

public class WinrateState
{
    private decimal potBB = 0m;
    private decimal investedBB = 0m;
    private decimal wonBB = 0m;
    private decimal heroStreetBet = 0m;
    private bool heroFolded = false;
    private bool wentToShowdown = false;

    public void Apply(HandHistoryEvent evnt, string heroPlayerId)
    {
        switch (evnt)
        {
            case AntePostEvent antePostEvent:
                Apply(antePostEvent, heroPlayerId);
                break;
            case BigBlindPostEvent bigBlindPostEvent:
                Apply(bigBlindPostEvent, heroPlayerId);
                break;
            case CashDropEvent cashDropEvent:
                Apply(cashDropEvent, heroPlayerId);
                break;
            case CardsShownEvent cardsShownEvent:
                Apply(cardsShownEvent, heroPlayerId);
                break;
            case FlopDealtEvent flopDealtEvent:
                Apply(flopDealtEvent, heroPlayerId);
                break;
            case PlayerBetEvent playerBetEvent:
                Apply(playerBetEvent, heroPlayerId);
                break;
            case PlayerCallEvent playerCallEvent:
                Apply(playerCallEvent, heroPlayerId);
                break;
            case PlayerCheckEvent playerCheckEvent:
                Apply(playerCheckEvent, heroPlayerId);
                break;
            case PlayerFoldEvent playerFoldEvent:
                Apply(playerFoldEvent, heroPlayerId);
                break;
            case PlayerRaiseEvent playerRaiseEvent:
                Apply(playerRaiseEvent, heroPlayerId);
                break;
            case PotAwardedEvent potAwardedEvent:
                Apply(potAwardedEvent, heroPlayerId);
                break;
            case RiverDealtEvent riverDealtEvent:
                Apply(riverDealtEvent, heroPlayerId);
                break;
            case SmallBlindPostEvent smallBlindPostEvent:
                Apply(smallBlindPostEvent, heroPlayerId);
                break;
            case TurnDealtEvent turnDealtEvent:
                Apply(turnDealtEvent, heroPlayerId);
                break;
            case UncalledBetReturnedEvent uncalledBetReturnedEvent:
                Apply(uncalledBetReturnedEvent, heroPlayerId);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(evnt),
                    evnt.GetType(),
                    "Unsupported hand-history event type."
                );
        }
    }

    public void Apply(AntePostEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            investedBB += evnt.AmountBB;
        }

        potBB += evnt.AmountBB;
    }

    public void Apply(BigBlindPostEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            investedBB += evnt.AmountBB;
            heroStreetBet += evnt.AmountBB;
        }

        potBB += evnt.AmountBB;
    }

    public void Apply(CashDropEvent evnt, string heroPlayerId)
    {
        potBB += evnt.AmountBB;
    }

    public void Apply(CardsShownEvent evnt, string heroPlayerId)
    {
        if (!heroFolded)
        {
            wentToShowdown = true;
        }
    }

    public void Apply(FlopDealtEvent evnt, string heroPlayerId)
    {
        ApplyBoardDealtEvent(evnt, heroPlayerId);
    }

    public void Apply(PlayerBetEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            investedBB += evnt.BetAmountBB;
            heroStreetBet += evnt.BetAmountBB;
        }

        potBB += evnt.BetAmountBB;
    }

    public void Apply(PlayerCallEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            investedBB += evnt.CallAmountBB;
            heroStreetBet += evnt.CallAmountBB;
        }

        potBB += evnt.CallAmountBB;
    }

    public void Apply(PlayerCheckEvent evnt, string heroPlayerId) { }

    public void Apply(PlayerFoldEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId && !wentToShowdown)
        {
            heroFolded = true;
        }
    }

    public void Apply(PlayerRaiseEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            var raiseContribution = evnt.RaiseToAmountBB - heroStreetBet;
            investedBB += raiseContribution;
            heroStreetBet += raiseContribution;
        }

        potBB += evnt.RaiseAmountBB;
    }

    public void Apply(PotAwardedEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            wonBB += evnt.AmountBB;
        }
    }

    public void Apply(RiverDealtEvent evnt, string heroPlayerId)
    {
        ApplyBoardDealtEvent(evnt, heroPlayerId);
    }

    public void Apply(SmallBlindPostEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            investedBB += evnt.AmountBB;
            heroStreetBet += evnt.AmountBB;
        }

        potBB += evnt.AmountBB;
    }

    public void Apply(TurnDealtEvent evnt, string heroPlayerId)
    {
        ApplyBoardDealtEvent(evnt, heroPlayerId);
    }

    public void Apply(UncalledBetReturnedEvent evnt, string heroPlayerId)
    {
        if (evnt.PlayerId == heroPlayerId)
        {
            investedBB -= evnt.AmountBB;
            heroStreetBet -= evnt.AmountBB;
        }
    }

    private void ApplyBoardDealtEvent(BoardDealtEvent evnt, string heroPlayerId)
    {
        heroStreetBet = 0m;
    }

    public Winrate GetWinrate()
    {
        var netWon = wonBB - investedBB;
        var netWonBeforeRake = wonBB > 0 ? potBB : 0;

        return new Winrate(
            netWon,
            wentToShowdown ? netWon : 0,
            wentToShowdown ? 0 : netWon,
            netWonBeforeRake,
            wentToShowdown ? netWonBeforeRake : 0,
            wentToShowdown ? 0 : netWonBeforeRake
        );
    }
}
