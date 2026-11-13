namespace PokerTrackerApi.Winrate;

public record WinrateDto(IReadOnlyList<WinrateDataPoint> Points);

public record WinrateDataPoint(
    int HandCount,
    decimal NetWinningsBB,
    decimal WithShowdownWinningsBB,
    decimal WithoutShowdownWinningsBB,
    decimal NetWinningsBeforeRakeBB,
    decimal WithShowdownWinningsBeforeRakeBB,
    decimal WithoutShowdownWinningsBeforeRakeBB
);
