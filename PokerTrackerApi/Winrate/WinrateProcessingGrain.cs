using PokerTrackerApi.Domain.HandHistories;
using PokerTrackerApi.Winrate.Repository;

namespace PokerTrackerApi.Winrate;

public interface IWinrateProcessingGrain : IGrainWithStringKey
{
    Task ProcessHandHistory(HandHistory hand, CancellationToken cancellationToken);
}

public class WinrateProcessingGrain(
    IWinrateRepository repository
) : Grain, IWinrateProcessingGrain
{
    public async Task ProcessHandHistory(HandHistory hand, CancellationToken cancellationToken)
    {
        var winrate = WinrateCalculator.CalulcateWinrate(hand);
        await repository.SaveWinrate(winrate, hand.HandId, cancellationToken);
    }
}
