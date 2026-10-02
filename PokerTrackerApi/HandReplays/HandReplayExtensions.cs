using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.InternalRepresentation;
using PokerTrackerApi.Domain.InternalRepresentation.Events;

namespace PokerTrackerApi.HandReplays;

public static class HandReplayExtensions
{
    public static HandReplay ToHandReplay(this ParsedHand hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        return new HandReplay
        {
            HandId = hand.HandId,
            HeroHoleCards = hand.HeroHoleCards,
            HeroPosition = hand.Players[hand.HeroPlayerId].Position,
        };
    }

    public static HandReplayPlayer[] ToHandReplayPlayers(this ParsedHand hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        return hand
            .Players.OrderBy(player => player.Value.Position)
            .Select(player => new HandReplayPlayer
            {
                HandId = hand.HandId,
                PlayerId = player.Key,
                Position = player.Value.Position,
                StartingStackBB = player.Value.StartingStackBB,
            })
            .ToArray();
    }

    public static HandReplayEvent[] ToHandReplayEvents(this ParsedHand hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        var street = PokerStreet.Preflop;
        var replayEvents = new List<HandReplayEvent>(hand.Events.Count);

        for (var sequence = 0; sequence < hand.Events.Count; sequence++)
        {
            var parsedEvent = hand.Events[sequence];
            if (parsedEvent is BoardDealt boardDealt)
            {
                street = boardDealt.Street;
            }

            replayEvents.Add(
                new HandReplayEvent
                {
                    HandId = hand.HandId,
                    Sequence = sequence,
                    Street = street.ToString(),
                    EventType = parsedEvent.GetType().Name,
                    PlayerId = GetPlayerId(parsedEvent),
                    AmountBB = GetAmountBB(parsedEvent),
                    RaiseToAmountBB = parsedEvent is PlayerRaiseEvent raise
                        ? raise.RaiseToAmountBB
                        : null,
                }
            );
        }

        return replayEvents.ToArray();
    }

    public static HandReplayEventCard[] ToHandReplayEventCards(this ParsedHand hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        return hand
            .Events.SelectMany(
                (parsedEvent, sequence) =>
                    GetCards(parsedEvent)
                        .Select(
                            (card, cardIndex) =>
                                new HandReplayEventCard
                                {
                                    HandId = hand.HandId,
                                    Sequence = sequence,
                                    CardIndex = cardIndex,
                                    Card = card,
                                }
                        )
            )
            .ToArray();
    }

    private static string? GetPlayerId(ParsedHandEvent parsedEvent) =>
        parsedEvent switch
        {
            PlayerActionEvent playerAction => playerAction.PlayerId,
            PostEvent post => post.PlayerId,
            CardsShown cardsShown => cardsShown.PlayerId,
            UncalledBetReturned returned => returned.PlayerId,
            PotAwarded potAwarded => potAwarded.PlayerId,
            _ => null,
        };

    private static decimal? GetAmountBB(ParsedHandEvent parsedEvent) =>
        parsedEvent switch
        {
            PostEvent post => post.AmountBB,
            PlayerCallEvent call => call.CallAmountBB,
            PlayerBetEvent bet => bet.BetAmountBB,
            PlayerRaiseEvent raise => raise.RaiseAmountBB,
            UncalledBetReturned returned => returned.AmountBB,
            PotAwarded potAwarded => potAwarded.AmountBB,
            CashDrop cashDrop => cashDrop.AmountBB,
            _ => null,
        };

    private static PlayingCard[] GetCards(ParsedHandEvent parsedEvent) =>
        parsedEvent switch
        {
            FlopDealt flop => [flop.First, flop.Second, flop.Third],
            TurnDealt turn => [turn.Card],
            RiverDealt river => [river.Card],
            CardsShown shown => [shown.HoleCards.First, shown.HoleCards.Second],
            _ => [],
        };
}
