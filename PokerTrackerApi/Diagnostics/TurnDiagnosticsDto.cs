namespace PokerTrackerApi.Diagnostics;

public record TurnDiagnosticsDto(IReadOnlyList<TurnSpotRowDto> Rows);

public record TurnSpotRowDto(
    string Spot,
    int Hands,
    decimal WinningsBBPer100,
    IReadOnlyList<string> HandIds,
    IReadOnlyList<TurnActionRowDto> ActionBreakdown
);

public record TurnActionRowDto(
    string Action,
    int Hands,
    decimal WinningsBBPer100,
    IReadOnlyList<string> HandIds
);

public enum TurnSpotType
{
    DelayedCBet,
    Barrel,
    Probe,
}

public enum TurnActionType
{
    Check,
    Bet,
}

public enum TurnPotType
{
    SRP,
    ThreeBet,
}
