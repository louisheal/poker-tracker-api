namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

/// <summary>
/// Represents a raise. <paramref name="RaiseAmountBB"/> is the amount added over the previous bet;
/// <paramref name="RaiseToAmountBB"/> is the resulting total bet, both in big blinds.
/// </summary>
public record PlayerRaiseEvent(string PlayerId, decimal RaiseAmountBB, decimal RaiseToAmountBB)
    : PlayerActionEvent(PlayerId);
