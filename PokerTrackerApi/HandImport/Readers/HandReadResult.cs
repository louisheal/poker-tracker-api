namespace PokerTrackerApi.HandImport.Readers;

public abstract record HandReadResult();
public record HandReadSuccess(string HandId, string RawText) : HandReadResult;
public record HandReadFailure(string RawText, string Error) : HandReadResult;
