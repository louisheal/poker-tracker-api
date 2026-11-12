namespace PokerTrackerApi.Domain.HandHistories;

[GenerateSerializer]
public record HandHistoryPlayer(
    [property: Id(0)] string PlayerId,
    [property: Id(1)] PokerPosition Position,
    [property: Id(2)] decimal StartingStackBB
);
