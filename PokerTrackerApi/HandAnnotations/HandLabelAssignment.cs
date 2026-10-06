using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandAnnotations;

public class HandLabelAssignment
{
    public required string HandId { get; init; }
    public required PokerStreet Street { get; init; }
    public required string Label { get; init; }
}
