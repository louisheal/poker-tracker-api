using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandFileSplitting;

public interface IHandFileSplittingGrain : IGrainWithGuidKey
{
    Task ProcessHandFileAsync(CancellationToken cancellationToken);
}

public class HandFileSplittingGrain : Grain, IHandFileSplittingGrain
{
    private readonly IGrainContext _grainContext;
    private readonly IHandFileStorage _handFileStorage;
    private readonly IHandFileSplitter _handFileSplitter;

    public HandFileSplittingGrain(
        IGrainContext grainContext,
        IHandFileStorage handFileStorage,
        IHandFileSplitter handFileSplitter
    )
    {
        _grainContext = grainContext;
        _handFileStorage = handFileStorage;
        _handFileSplitter = handFileSplitter;
    }

    public async Task ProcessHandFileAsync(CancellationToken cancellationToken)
    {
        var fileId = _grainContext.GrainId.GetGuidKey();
        await using var handFile = await _handFileStorage.OpenReadAsync(fileId, cancellationToken);

        await foreach (var rawHand in _handFileSplitter.SplitHandsAsync(handFile, cancellationToken))
        {
            // skip if hand id already processed

            // publish raw hand string to kafka
                // TODO : create RawHand kafka message record

            // update grain storage with hand id checkpoint

        }
    }
}
