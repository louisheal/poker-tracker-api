namespace PokerTrackerApi.Diagnostics;

public record RiverDiagnosticsDto(IReadOnlyList<RiverSpotRowDto> Rows);

public record RiverSpotRowDto(
    string Spot,
    int Hands,
    decimal WinningsBBPer100,
    IReadOnlyList<RiverBetSizeRowDto> SizeBreakdown,
    IReadOnlyList<string> HandIds
);

public record RiverBetSizeRowDto(
    string Size,
    int Hands,
    decimal WinningsBBPer100,
    IReadOnlyList<string> HandIds
);
