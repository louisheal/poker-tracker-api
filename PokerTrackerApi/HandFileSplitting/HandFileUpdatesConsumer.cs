using DotNetCore.CAP;
using PokerTrackerApi.Domain;

namespace PokerTrackerApi.HandFileSplitting;

public class HandFileUpdatesConsumer
{
    private readonly IGrainFactory _grainFactory;

    public HandFileUpdatesConsumer(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [CapSubscribe("HandFileUpdates", Group = "hand-splitting")]
    public Task HandleAsync(
        HandFileStored message,
        CancellationToken cancellationToken
    )
    {
        var grain = _grainFactory.GetGrain<IHandFileSplittingGrain>(message.FileId);
        return grain.ProcessHandFileAsync(cancellationToken);
    }
}
