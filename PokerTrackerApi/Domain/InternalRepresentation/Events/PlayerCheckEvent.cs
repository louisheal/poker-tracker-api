namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record PlayerCheckEvent(string PlayerId) : PlayerActionEvent(PlayerId);
