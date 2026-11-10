using PokerTrackerApi.Domain.HandHistories;

namespace PokerTrackerApi.Domain.Messages;

// TODO : we don't need HandId - it's already in HandHistory
// TODO : does HandHistory just become the kafka message?
// or is it fine having a wrapper there
public record HandHistoryParsed(string HandId, HandHistory HandHistory);
