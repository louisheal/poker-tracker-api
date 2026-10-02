namespace PokerTrackerApi.HandParsing;

using PokerTrackerApi.Domain.InternalRepresentation;

public abstract record HandHistoryParseResult(ParsedHand? Hand, string? error);

public record HandHistoryParseSuccess(ParsedHand Hand) : HandHistoryParseResult(Hand, null);

public record HandHistoryParseFailure(string Error) : HandHistoryParseResult(null, Error);
