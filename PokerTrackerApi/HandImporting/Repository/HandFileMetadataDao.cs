namespace PokerTrackerApi.HandImporting.Repository;

public class HandFileMetadataDao
{
    public Guid FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public HandFileStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
