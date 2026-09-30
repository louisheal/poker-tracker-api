namespace PokerTrackerApi.HandHistories.Imports;

public sealed record HandHistoryImportSummary(
    int FilesReceived,
    int HandsSaved,
    int DuplicateHands,
    int InvalidHands);