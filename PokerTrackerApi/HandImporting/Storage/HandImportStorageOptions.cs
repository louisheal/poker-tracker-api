namespace PokerTrackerApi.HandImporting.Storage;

public class HandImportStorageOptions
{
    public const string SectionName = "HandImportStorage";

    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
