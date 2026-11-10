namespace PokerTrackerApi.HandParsing;

using PokerTrackerApi.Domain.HandHistories;

public abstract record HandHistoryParseResult(HandHistory? Hand, string? error);

public record HandHistoryParseSuccess(HandHistory Hand) : HandHistoryParseResult(Hand, null);

public record HandHistoryParseFailure(string Error) : HandHistoryParseResult(null, Error);
