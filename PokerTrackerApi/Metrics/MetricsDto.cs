namespace PokerTrackerApi.Metrics;

public record MetricsDto(
    int HandsPlayed,
    decimal WinningsBBPer100,
    decimal WentToShowdownPercent,
    decimal WonMoneyAtShowdownPercent
);
