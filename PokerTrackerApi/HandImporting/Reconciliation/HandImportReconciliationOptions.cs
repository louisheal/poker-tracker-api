namespace PokerTrackerApi.HandImporting.Reconciliation;

public sealed class HandImportReconciliationOptions
{
    public const string SectionName = "HandImportReconciliation";

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromHours(1);
    public int BatchSize { get; set; } = 100;
}
