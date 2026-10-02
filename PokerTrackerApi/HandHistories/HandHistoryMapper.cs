using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistoryMapper
{
    HoleCardsDto Map(HoleCards holeCards);
}

public class HandHistoryMapper : IHandHistoryMapper
{
    public HoleCardsDto Map(HoleCards holeCards)
    {
        var firstRank = holeCards.First.Rank.ToCode();
        var firstSuit = holeCards.First.Suit.ToCode();
        var firstCard = new PlayingCardDto(firstRank, firstSuit);

        var secondRank = holeCards.Second.Rank.ToCode();
        var secondSuit = holeCards.Second.Suit.ToCode();
        var secondCard = new PlayingCardDto(secondRank, secondSuit);

        return new HoleCardsDto(firstCard, secondCard);
    }
}
