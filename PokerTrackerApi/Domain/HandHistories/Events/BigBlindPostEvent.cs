namespace PokerTrackerApi.Domain.HandHistories.Events;

public record BigBlindPostEvent(
    int Sequence,
    string PlayerId,
    PostType PostType,
    decimal AmountBB
) : PostEvent(Sequence, PlayerId, PostType, AmountBB);
