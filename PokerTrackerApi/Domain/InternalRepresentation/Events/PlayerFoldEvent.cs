namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record PlayerFoldEvent(string PlayerId) : PlayerActionEvent(PlayerId);
