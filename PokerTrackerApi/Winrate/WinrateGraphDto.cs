namespace PokerTrackerApi.Winrate;

public record WinrateGraphDto(IReadOnlyList<WinrateHandBatchPointDto> Points);

public record WinrateHandBatchPointDto(
    int HandCount,
    decimal NetWinningsBB,
    decimal WithShowdownWinningsBB,
    decimal WithoutShowdownWinningsBB
)
{
    public decimal NetWinningsBBMovingAverage { get; init; }

    public decimal WithShowdownWinningsBBMovingAverage { get; init; }

    public decimal WithoutShowdownWinningsBBMovingAverage { get; init; }
}
