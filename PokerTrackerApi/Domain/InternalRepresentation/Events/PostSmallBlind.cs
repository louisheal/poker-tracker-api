namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record PostSmallBlind(string PlayerId, decimal AmountBB)
    : PostEvent(PlayerId, PostType.SmallBlind, AmountBB);
