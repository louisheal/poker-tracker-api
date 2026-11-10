namespace PokerTrackerApi.Domain.HandHistories.Events;

public record CardsShownEvent(int Sequence, string PlayerId, HoleCards HoleCards)
    : HandHistoryEvent(Sequence);
