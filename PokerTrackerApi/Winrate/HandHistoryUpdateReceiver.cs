using DotNetCore.CAP;
using PokerTrackerApi.Domain.Messages;

namespace PokerTrackerApi.Winrate;

public class HandHistoryUpdateReceiver : ICapSubscribe
{
    private readonly IGrainFactory _grainFactory;

    public HandHistoryUpdateReceiver(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [CapSubscribe(PokerTrackerNames.HandHistoryUpdatesTopic, Group = "winrate")]
    public async Task HandleAsync(HandHistoryParsed message, CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<IWinrateProcessingGrain>(message.HandId);
        await grain.ProcessHandHistory(message.HandHistory, cancellationToken);
    }
}
