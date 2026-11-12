namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record BigBlindPostEvent(
    int Sequence,
    string PlayerId,
    PostType PostType,
    decimal AmountBB
) : PostEvent(Sequence, PlayerId, PostType, AmountBB);
