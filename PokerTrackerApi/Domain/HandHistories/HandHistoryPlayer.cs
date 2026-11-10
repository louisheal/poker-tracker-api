namespace PokerTrackerApi.Domain.HandHistories;

public record HandHistoryPlayer(string PlayerId, PokerPosition Position, decimal StartingStackBB);
