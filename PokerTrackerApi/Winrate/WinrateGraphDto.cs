namespace PokerTrackerApi.Winrate;

public record WinrateGraphDto(IReadOnlyList<WinrateHandBatchPointDto> Points);

public record WinrateHandBatchPointDto(
    int HandCount,
    decimal NetWinningsBB,
    decimal WithShowdownWinningsBB,
    decimal WithoutShowdownWinningsBB
);
