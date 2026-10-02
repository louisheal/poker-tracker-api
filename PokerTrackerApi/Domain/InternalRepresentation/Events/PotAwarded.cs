namespace PokerTrackerApi.Domain.InternalRepresentation.Events;

/// <summary>
/// Represents the amount a player collected from the pot after rake and jackpot deductions, in big blinds.
/// </summary>
public record PotAwarded(string PlayerId, decimal AmountBB) : ParsedHandEvent();
