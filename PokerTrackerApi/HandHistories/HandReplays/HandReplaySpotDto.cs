namespace PokerTrackerApi.HandHistories.HandReplays;

public record HandReplaySpotDto(
    decimal PotBB,
    IReadOnlyList<string> ActivePlayers,
    string? NextToAct,
    IReadOnlyDictionary<string, decimal> PlayerBets,
    IReadOnlyList<PlayingCardDto> Board
);
