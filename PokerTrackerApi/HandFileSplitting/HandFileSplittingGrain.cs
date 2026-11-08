using DotNetCore.CAP;
using PokerTrackerApi.Domain;
using PokerTrackerApi.HandFileMetadata;
using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandFileSplitting;

public interface IHandFileSplittingGrain : IGrainWithGuidKey
{
    Task ProcessHandFileAsync(CancellationToken cancellationToken);
}

public class HandFileSplittingGrain : Grain, IHandFileSplittingGrain
{
    // TODO : move into consts names
    private const string TOPIC_NAME = "RawHandUpdates";

    private readonly IGrainContext _grainContext;
    private readonly IPersistentState<HandFileSplittingGrainState> _state;
    private readonly IHandFileStorage _handFileStorage;
    private readonly IHandFileSplitter _handFileSplitter;
    private readonly IHandFileMetadataRepository _repository;
    private readonly ICapPublisher _capPublisher;

    public HandFileSplittingGrain(
        [PersistentState("HandFileSplittingGrainState", "Default")]
            IPersistentState<HandFileSplittingGrainState> state,
        IGrainContext grainContext,
        IHandFileStorage handFileStorage,
        IHandFileSplitter handFileSplitter,
        IHandFileMetadataRepository repository,
        ICapPublisher capPublisher
    )
    {
        _state = state;
        _grainContext = grainContext;
        _handFileStorage = handFileStorage;
        _handFileSplitter = handFileSplitter;
        _repository = repository;
        _capPublisher = capPublisher;
    }

    // TODO : track isCompleted so you can skip replays
    public async Task ProcessHandFileAsync(CancellationToken cancellationToken)
    {
        var isPastCheckpoint = false;

        var fileId = _grainContext.GrainId.GetGuidKey();
        await using var handFile = await _handFileStorage.OpenReadAsync(fileId, cancellationToken);

        await foreach (var rawHand in _handFileSplitter.SplitHandsAsync(handFile, cancellationToken))
        {
            if (!isPastCheckpoint)
            {
                isPastCheckpoint = _state.State.LastProcessedHandId == null ||
                    rawHand.HandId == _state.State.LastProcessedHandId;
                continue;
            }

            await _capPublisher.PublishAsync(
                TOPIC_NAME,
                new HandExtracted(rawHand.RawText),
                cancellationToken: cancellationToken
            );

            var state = _state.State with { LastProcessedHandId = rawHand.HandId };
            await SaveState(state, cancellationToken);
        }

        await _handFileStorage.RemoveAsync(fileId, cancellationToken);
        await _repository.MarkCompletedAsync(fileId, cancellationToken);
    }

    private async Task SaveState(HandFileSplittingGrainState state, CancellationToken cancellationToken)
    {
        try
        {
            _state.State = state;
            await _state.WriteStateAsync(cancellationToken);
        }
        catch
        {
            DeactivateOnIdle();
            throw;
        }
    }

    public record HandFileSplittingGrainState
    {
        public string? LastProcessedHandId { get; set; } = null;
    }
}
