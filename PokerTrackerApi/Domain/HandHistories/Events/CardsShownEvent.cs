namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record CardsShownEvent(
    int Sequence,
    [property: Id(1)] string PlayerId,
    [property: Id(2)] HoleCards HoleCards
)
    : HandHistoryEvent(Sequence);
