namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

public record PlayerBetEvent(string PlayerId, decimal BetAmountBB) : PlayerActionEvent(PlayerId);
