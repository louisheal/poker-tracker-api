namespace PokerTrackerApi.HandAnnotations;

public record HandLabelOption(string Value, string Name, string Category);

public static class HandLabelCatalog
{
    public static IReadOnlyList<HandLabelOption> All { get; } =
    [
        new("bad-bluffs", "Bad Bluffs", "Red"),
        new("bad-value-bet", "Bad Value Bet", "Red"),
        new("bad-logic", "Bad Logic", "Red"),
        new("ignoring-relative-strength", "Ignoring Relative Strength", "Blue"),
        new("calling-underbluffed-line", "Calling Underbluffed Line", "Blue"),
        new("ego-calling", "Ego Calling", "Blue"),
        new("missed-value", "Missed Value", "Green"),
        new("missed-bluff", "Missed Bluff", "Green"),
        new("shrug-fold", "Shrug Fold", "Green"),
        new("no-leak", "No Leak", "Purple"),
    ];

    public static bool Contains(string value) => All.Any(option => option.Value == value);
}
