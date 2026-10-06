namespace PokerTrackerApi.Metrics;

public record HeroHandResult(
    decimal NetWinningsBB,
    bool SawFlop,
    bool WentToShowdown,
    bool WonAtShowdown
);
