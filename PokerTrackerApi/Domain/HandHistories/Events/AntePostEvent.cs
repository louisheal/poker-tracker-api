namespace PokerTrackerApi.Domain.HandHistories.Events;

public record AntePostEvent(
    int Sequence,
    string PlayerId,
    PostType PostType,
    decimal AmountBB
) : PostEvent(Sequence, PlayerId, PostType, AmountBB);
