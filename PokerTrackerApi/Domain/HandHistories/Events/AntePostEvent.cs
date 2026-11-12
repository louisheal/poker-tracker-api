namespace PokerTrackerApi.Domain.HandHistories.Events;

[GenerateSerializer]
public record AntePostEvent(
    int Sequence,
    string PlayerId,
    PostType PostType,
    decimal AmountBB
) : PostEvent(Sequence, PlayerId, PostType, AmountBB);
