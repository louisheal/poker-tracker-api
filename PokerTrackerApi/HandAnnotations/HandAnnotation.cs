namespace PokerTrackerApi.HandAnnotations;

public class HandAnnotation
{
    public required string HandId { get; init; }
    public required string Note { get; set; }
    public bool Flagged { get; set; }
}
