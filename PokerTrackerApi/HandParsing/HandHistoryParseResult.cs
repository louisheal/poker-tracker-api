namespace PokerTrackerApi.HandParsing;

using PokerTrackerApi.Domain.InternalRepresentation;

public abstract record HandHistoryParseResult(ParsedHandIr? Hand, string? error);

public record HandHistoryParseSuccess(ParsedHandIr Hand) : HandHistoryParseResult(Hand, null);

public record HandHistoryParseFailure(string Error) : HandHistoryParseResult(null, Error);
