namespace PokerTrackerApi.HandParsing;

using PokerTrackerApi.Domain.PokerHand;

public abstract record HandHistoryParseResult(PokerHand? Hand, string? error);

public record HandHistoryParseSuccess(PokerHand Hand) : HandHistoryParseResult(Hand, null);

public record HandHistoryParseFailure(string Error) : HandHistoryParseResult(null, Error);
