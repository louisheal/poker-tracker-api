using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Domain.PokerHand.Events;

namespace PokerTrackerApi.MassData;

public class PostflopBettingSpotExtractorStateful
{
    private PokerStreet _pokerStreet = PokerStreet.Preflop;
    private HashSet<string> _activePlayers = [];
    private decimal _potAmountBB = 0m;

    public IReadOnlyList<PostflopBettingSpot> Extract(PokerHand hand)
    {
        InitialiseState(hand);

        var handEvents = hand.Events.OrderBy(handEvent => handEvent.Sequence);

        foreach (var handEvent in handEvents)
        {
            switch (handEvent)
            {
                case PokerHandSmallBlindPostEvent smallBlindPostEvent:
                    HandleSmallBlindPostEvent(smallBlindPostEvent);
                    break;
                case PokerHandBigBlindPostEvent bigBlindPostEvent:
                    HandleBigBlindPostEvent(bigBlindPostEvent);
                    break;
                case PokerHandPlayerFoldEvent playerFoldEvent:
                    HandlePlayerFoldEvent(playerFoldEvent);
                    break;
                case PokerHandPlayerBetEvent playerBetEvent:
                    HandlePlayerBetEvent(playerBetEvent);
                    break;
                case PokerHandFlopDealtEvent flopDealtEvent:
                    HandleFlopDealtEvent(flopDealtEvent);
                    break;
                case PokerHandTurnDealtEvent turnDealtEvent:
                    HandleTurnDealtEvent(turnDealtEvent);
                    break;
                case PokerHandRiverDealtEvent riverDealtEvent:
                    HandleRiverDealtEvent(riverDealtEvent);
                    break;
            }
        }

        return [];
    }

    // TODO : increase line length limit
    private HandlerResult HandleSmallBlindPostEvent(
        PokerHandSmallBlindPostEvent smallBlindPostEvent
    )
    {
        _potAmountBB += smallBlindPostEvent.AmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandleBigBlindPostEvent(PokerHandBigBlindPostEvent bigBlindPostEvent)
    {
        _potAmountBB += bigBlindPostEvent.AmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerFoldEvent(PokerHandPlayerFoldEvent playerFoldEvent)
    {
        _activePlayers.Remove(playerFoldEvent.PlayerId);
        return HandlerResult.Valid;
    }

    private HandlerResult HandlePlayerBetEvent(PokerHandPlayerBetEvent playerBetEvent)
    {
        _potAmountBB += playerBetEvent.BetAmountBB;
        return HandlerResult.Valid;
    }

    private HandlerResult HandleFlopDealtEvent(PokerHandFlopDealtEvent flopDealtEvent)
    {
        if (_activePlayers.Count() != 2)
        {
            // TODO : skip entire hand
            // each handler could return a tuple (isValid, result)
            // we can yield return result
            // and break if not valid

            // TODO : can we make this into a factory or something?
            return HandlerResult.Invalid;
        }

        _pokerStreet = PokerStreet.Flop;
        return HandlerResult.Valid;
    }

    private HandlerResult HandleTurnDealtEvent(PokerHandTurnDealtEvent turnDealtEvent)
    {
        _pokerStreet = PokerStreet.Turn;
        return HandlerResult.Valid;
    }

    private HandlerResult HandleRiverDealtEvent(PokerHandRiverDealtEvent riverDealtEvent)
    {
        _pokerStreet = PokerStreet.River;
        return HandlerResult.Valid;
    }

    private void InitialiseState(PokerHand hand)
    {
        _pokerStreet = PokerStreet.Preflop;
        _activePlayers = hand
            .Players.Select(player => player.PlayerId)
            .ToHashSet(StringComparer.Ordinal);
        _potAmountBB = 0;
    }

    private record HandlerResult(bool IsValid)
    {
        public static HandlerResult Valid = new(true);
        public static HandlerResult Invalid = new(false);
    }
}
