namespace PokerTrackerApi.HandFileStorage;

public class HandFileStorageOptions
{
    public const string SectionName = "HandFileStorage";

    public string Endpoint { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
