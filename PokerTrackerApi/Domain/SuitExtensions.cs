namespace PokerTrackerApi.Domain;

public static class SuitExtensions
{
    public static string ToCode(this Suit suit) => suit switch
    {
        Suit.Diamonds => "d",
        Suit.Hearts => "h",
        Suit.Clubs => "c",
        Suit.Spades => "s",
        _ => throw new ArgumentException($"Unknown suit '{suit}'.", nameof(suit)),
    };

    public static Suit FromCode(char code) => code switch
    {
        'd' => Suit.Diamonds,
        'h' => Suit.Hearts,
        'c' => Suit.Clubs,
        's' => Suit.Spades,
        _ => throw new ArgumentException($"Unknown suit code '{code}'.", nameof(code)),
    };
}