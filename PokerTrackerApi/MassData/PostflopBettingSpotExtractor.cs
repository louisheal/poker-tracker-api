using PokerTrackerApi.Domain.PokerHand;

namespace PokerTrackerApi.MassData;

public partial class PostflopBettingSpotExtractor
{
    public IReadOnlyList<PostflopBettingSpot> Extract(PokerHand hand)
    {
        var state = new ExtractorState(hand);
        foreach (var handEvent in hand.Events.OrderBy(handEvent => handEvent.Sequence))
        {
            if (!state.Handle(handEvent))
            {
                return [];
            }
        }

        return state.BuildSpots();
    }
}
