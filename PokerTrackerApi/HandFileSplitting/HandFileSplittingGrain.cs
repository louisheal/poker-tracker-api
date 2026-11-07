using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandSplitting;

public interface IHandFileSplittingGrain : IGrainWithGuidKey
{
    Task ProcessHandFileAsync(CancellationToken cancellationToken);
}

public class HandFileSplittingGrain : Grain, IHandFileSplittingGrain
{
    private readonly IGrainContext _grainContext;
    private readonly IHandFileStorage _handFileStorage;

    public HandFileSplittingGrain(
        IGrainContext grainContext,
        IHandFileStorage handFileStorage
    )
    {
        _grainContext = grainContext;
        _handFileStorage = handFileStorage;
    }

    public async Task ProcessHandFileAsync(CancellationToken cancellationToken)
    {
        var fileId = _grainContext.GrainId.GetGuidKey();
        await using var handFile = await _handFileStorage.OpenReadAsync(fileId, cancellationToken);
    }
}
