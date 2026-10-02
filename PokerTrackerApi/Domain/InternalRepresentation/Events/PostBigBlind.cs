namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record PostBigBlind(string PlayerId, decimal AmountBB)
    : PostEvent(PlayerId, PostType.BigBlind, AmountBB);
