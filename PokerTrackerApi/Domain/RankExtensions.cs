namespace PokerTrackerApi.Domain;

public static class RankExtensions
{
    public static string ToCode(this Rank rank) => rank switch
    {
        Rank.Two => "2",
        Rank.Three => "3",
        Rank.Four => "4",
        Rank.Five => "5",
        Rank.Six => "6",
        Rank.Seven => "7",
        Rank.Eight => "8",
        Rank.Nine => "9",
        Rank.Ten => "T",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        Rank.Ace => "A",
        _ => throw new ArgumentOutOfRangeException(nameof(rank), rank, null),
    };

    public static Rank FromCode(char code) => code switch
    {
        '2' => Rank.Two,
        '3' => Rank.Three,
        '4' => Rank.Four,
        '5' => Rank.Five,
        '6' => Rank.Six,
        '7' => Rank.Seven,
        '8' => Rank.Eight,
        '9' => Rank.Nine,
        'T' => Rank.Ten,
        'J' => Rank.Jack,
        'Q' => Rank.Queen,
        'K' => Rank.King,
        'A' => Rank.Ace,
        _ => throw new ArgumentException($"Unknown rank code '{code}'.", nameof(code)),
    };
}