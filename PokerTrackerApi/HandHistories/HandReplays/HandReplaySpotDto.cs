namespace PokerTrackerApi.HandHistories.HandReplays;

public record HandReplaySpotDto(
    decimal PotBB,
    IReadOnlyList<string> ActivePlayers,
    string? NextToAct,
    IReadOnlyDictionary<string, decimal> PlayersBetsBB,
    IReadOnlyDictionary<string, decimal> RemainingStacksBB,
    IReadOnlyDictionary<string, HoleCardsDto> RevealedHoleCards,
    string Street,
    IReadOnlyList<PlayingCardDto> Board,
    IReadOnlyDictionary<string, decimal> WinningsBB
);
