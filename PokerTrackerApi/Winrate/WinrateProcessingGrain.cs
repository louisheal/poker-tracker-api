using PokerTrackerApi.Domain.HandHistories;

namespace PokerTrackerApi.Winrate;

public interface IWinrateProcessingGrain : IGrainWithStringKey
{
    Task ProcessHandHistory(HandHistory hand, CancellationToken cancellationToken);
}

public class WinrateProcessingGrain : Grain, IWinrateProcessingGrain
{
    public async Task ProcessHandHistory(HandHistory hand, CancellationToken cancellationToken)
    {
        var winrate = WinrateCalculator.CalulcateWinrate(hand);
    }
}
