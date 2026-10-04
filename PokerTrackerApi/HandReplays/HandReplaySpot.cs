using PokerTrackerApi.Contract;

namespace PokerTrackerApi.HandReplays;

public record HandReplaySpot(
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
