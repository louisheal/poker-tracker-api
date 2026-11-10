using DotNetCore.CAP;
using PokerTrackerApi.Domain.Messages;
using PokerTrackerApi.HandFileMetadata;
using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandFileSplitting;

public interface IHandFileSplittingGrain : IGrainWithGuidKey
{
    Task ProcessHandFileAsync(CancellationToken cancellationToken);
}

public class HandFileSplittingGrain : Grain, IHandFileSplittingGrain
{
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

    public async Task ProcessHandFileAsync(CancellationToken cancellationToken)
    {
        var fileId = _grainContext.GrainId.GetGuidKey();

        if (!_state.State.IsCompleted)
        {
            await Handle(fileId, cancellationToken);
        }

        await _handFileStorage.RemoveAsync(fileId, cancellationToken);
        await _repository.MarkCompletedAsync(fileId, cancellationToken);
        
        var state = _state.State with { IsCompleted = true };
        await SaveState(state, cancellationToken);
    }

    private async Task Handle(Guid fileId, CancellationToken cancellationToken)
    {
        await using var handFile = await _handFileStorage.OpenReadAsync(fileId, cancellationToken);

        var isPastCheckpoint = false;
        await foreach (var rawHand in _handFileSplitter.SplitHandsAsync(handFile, cancellationToken))
        {
            if (!isPastCheckpoint)
            {
                isPastCheckpoint = _state.State.LastProcessedHandId == null ||
                    rawHand.HandId == _state.State.LastProcessedHandId;
                continue;
            }

            // TODO : save raw text to db for reprocessing

            await _capPublisher.PublishAsync(
                PokerTrackerNames.RawHandUpdatesTopic,
                new HandExtracted(rawHand.HandId, rawHand.RawText),
                cancellationToken: cancellationToken
            );

            var state = _state.State with { LastProcessedHandId = rawHand.HandId };
            await SaveState(state, cancellationToken);
        }
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

    [GenerateSerializer]
    public record HandFileSplittingGrainState
    {
        [Id(0)]
        public string? LastProcessedHandId { get; set; } = null;

        [Id(1)]
        public bool IsCompleted { get; set; } = false;
    }
}
