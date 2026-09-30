namespace PokerTrackerApi.HandHistories.Imports;

public interface IHandHistoryImportService
{
    Task<HandHistoryImportSummary> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken);
}