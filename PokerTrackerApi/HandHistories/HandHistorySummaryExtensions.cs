using PokerTrackerApi.Domain.InternalRepresentation;

namespace PokerTrackerApi.HandHistories;

public static class HandHistorySummaryExtensions
{
    public static HandHistorySummary ToHandHistorySummary(this ParsedHand hand)
    {
        ArgumentNullException.ThrowIfNull(hand);

        return new HandHistorySummary
        {
            HandId = hand.HandId,
            HoleCards = hand.HeroHoleCards,
            HeroPosition = hand.Players[hand.HeroPlayerId].Position,
        };
    }
}
