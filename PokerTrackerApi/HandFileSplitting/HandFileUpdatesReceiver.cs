using DotNetCore.CAP;
using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandFileSplitting;

public class HandFileUpdatesReceiver : ICapSubscribe
{
    private readonly IGrainFactory _grainFactory;

    public HandFileUpdatesReceiver(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [CapSubscribe("HandFileUpdates", Group = "hand-splitting")]
    public async Task HandleAsync(HandFileStored message, CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<IHandFileSplittingGrain>(message.FileId);
        await grain.ProcessHandFileAsync(cancellationToken);
    }
}
