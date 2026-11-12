namespace PokerTrackerApi.Winrate;

public record Winrate(
    decimal NetWonBB,
    decimal WonWithShowdownBB,
    decimal WonWithoutShowdownBB,
    decimal NetWonBeforeRakeBB,
    decimal WonWithShowdownBeforeRakeBB,
    decimal WonWithoutShowdownBeforeRakeBB
);
