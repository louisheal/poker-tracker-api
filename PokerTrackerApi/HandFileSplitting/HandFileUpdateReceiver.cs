using DotNetCore.CAP;
using PokerTrackerApi.Domain.Messages;

namespace PokerTrackerApi.HandFileSplitting;

public class HandFileUpdateReceiver : ICapSubscribe
{
    private readonly IGrainFactory _grainFactory;

    public HandFileUpdateReceiver(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    [CapSubscribe(PokerTrackerNames.HandFileUpdatesTopic, Group = "hand-splitting")]
    public async Task HandleAsync(HandFileStored message, CancellationToken cancellationToken)
    {
        var grain = _grainFactory.GetGrain<IHandFileSplittingGrain>(message.FileId);
        await grain.ProcessHandFileAsync(cancellationToken);
    }
}
