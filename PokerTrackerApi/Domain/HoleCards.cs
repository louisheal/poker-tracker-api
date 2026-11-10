namespace PokerTrackerApi.Domain;

public record HoleCards(PlayingCard First, PlayingCard Second)
{
    public string HandKey()
    {
        var firstRank = First.Rank;
        var secondRank = Second.Rank;
        if (firstRank == secondRank)
        {
            return $"{firstRank.ToCode()}{secondRank.ToCode()}";
        }

        var firstIsHigher = firstRank > secondRank;
        var highCard = firstIsHigher ? First : Second;
        var lowCard = firstIsHigher ? Second : First;
        var suitedness = highCard.Suit == lowCard.Suit ? "s" : "o";
        return $"{highCard.Rank.ToCode()}{lowCard.Rank.ToCode()}{suitedness}";
    }

    public static HoleCards FromCodes(string first, string second)
    {
        var firstRank = RankExtensions.FromCode(first[0]);
        var firstSuit = SuitExtensions.FromCode(first[1]);
        var firstCard = new PlayingCard(firstRank, firstSuit);

        var secondRank = RankExtensions.FromCode(second[0]);
        var secondSuit = SuitExtensions.FromCode(second[1]);
        var secondCard = new PlayingCard(secondRank, secondSuit);

        return new HoleCards(firstCard, secondCard);
    }
}
