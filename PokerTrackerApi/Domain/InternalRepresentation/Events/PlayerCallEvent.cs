namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record PlayerCallEvent(string PlayerId, decimal CallAmountBB) : PlayerActionEvent(PlayerId);
