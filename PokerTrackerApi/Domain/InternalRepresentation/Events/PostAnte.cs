namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record PostAnte(string PlayerId, decimal AmountBB)
    : PostEvent(PlayerId, PostType.Ante, AmountBB);