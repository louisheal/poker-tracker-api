using PokerTrackerApi.Domain.HandHistories;

namespace PokerTrackerApi.Domain.Messages;

public record HandParsed(string HandId, HandHistory HandHistory);
