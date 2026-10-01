namespace PokerTrackerApi.Domain;

public static class SuitExtensions
{
    public static Suit FromCode(char code) => code switch
    {
        'd' => Suit.Diamonds,
        'h' => Suit.Hearts,
        'c' => Suit.Clubs,
        's' => Suit.Spades,
        _ => throw new ArgumentException($"Unknown suit code '{code}'.", nameof(code)),
    };
}