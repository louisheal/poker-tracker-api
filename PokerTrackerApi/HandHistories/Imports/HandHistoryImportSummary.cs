namespace PokerTrackerApi.HandHistories.Imports;

public record HandHistoryImportSummary(
    int FilesReceived,
    int HandsSaved,
    int DuplicateHands,
    int InvalidHands);