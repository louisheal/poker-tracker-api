using System.Threading.Channels;
using System.Runtime.ExceptionServices;
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
        var pendingCheckpoints = Channel.CreateUnbounded<int>();
        var checkpointTask = DoCheckpoints(pendingCheckpoints, cancellationToken);
        var publishTasks = new List<Task>();

        var handlingError = await CaptureError(() => PublishHands(fileId, pendingCheckpoints, publishTasks, cancellationToken));
        var publishError = await CaptureError(() => Task.WhenAll(publishTasks));

        pendingCheckpoints.Writer.TryComplete();
        var checkpointError = await CaptureError(() => checkpointTask);

        ThrowIfErrors(
            ("hand handling", handlingError),
            ("publishing", publishError),
            ("checkpointing", checkpointError)
        );
    }

    private async Task PublishHands(
        Guid fileId,
        Channel<int> pendingCheckpoints,
        List<Task> publishTasks,
        CancellationToken cancellationToken
    )
    {
        await using var handFile = await _handFileStorage.OpenReadAsync(fileId, cancellationToken);

        await foreach (var rawHand in _handFileSplitter.SplitHandsAsync(handFile, cancellationToken))
        {
            if (_state.State.Checkpoint >= rawHand.Sequence)
            {
                continue;
            }

            publishTasks.Add(
                PublishAndSaveState(
                    rawHand.HandId,
                    rawHand.RawText,
                    rawHand.Sequence,
                    pendingCheckpoints,
                    cancellationToken
                )
            );
        }
    }

    private async Task PublishAndSaveState(
        string handId,
        string rawText,
        int id,
        Channel<int> pendingCheckpoints,
        CancellationToken cancellationToken
    )
    {
        // TODO : save raw text to db for reprocessing

        await _capPublisher.PublishAsync(
            PokerTrackerNames.RawHandUpdatesTopic,
            new HandExtracted(handId, rawText),
            cancellationToken: cancellationToken
        );

        await pendingCheckpoints.Writer.WriteAsync(id, cancellationToken);
    }

    private async Task DoCheckpoints(
        Channel<int> pendingCheckpoints,
        CancellationToken cancellationToken
    )
    {
        var checkpointSet = new HashSet<int>();
        var nextCheckpoint = _state.State.Checkpoint + 1;

        await foreach (var id in pendingCheckpoints.Reader.ReadAllAsync(cancellationToken))
        {
            HandFileSplittingGrainState? state = null;

            checkpointSet.Add(id);
            while (checkpointSet.Contains(nextCheckpoint))
            {
                state = _state.State with { Checkpoint = nextCheckpoint };
                checkpointSet.Remove(nextCheckpoint);
                nextCheckpoint++;
            }

            if (state != null)
            {
                await SaveState(state, cancellationToken);
            }
        }
    }

    private async Task SaveState(
        HandFileSplittingGrainState state,
        CancellationToken cancellationToken
    )
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

    private static async Task<Exception?> CaptureError(Func<Task> operation)
    {
        try
        {
            await operation();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static void ThrowIfErrors(params (string Stage, Exception? Error)[] errors)
    {
        var capturedErrors = errors
            .Where(error => error.Error is not null)
            .Select(error => (error.Stage, Error: error.Error!))
            .ToArray();

        if (capturedErrors.Length == 0)
        {
            return;
        }

        if (capturedErrors.Length == 1)
        {
            ExceptionDispatchInfo.Capture(capturedErrors[0].Error).Throw();
        }

        var handlingError = capturedErrors[0];
        var additionalError = capturedErrors[1];
        throw new InvalidOperationException(
            $"Encountered {additionalError.Stage} error '{additionalError.Error.Message}' while handling {handlingError.Stage} error '{handlingError.Error.Message}'.",
            new AggregateException(capturedErrors.Select(error => error.Error))
        );
    }

    [GenerateSerializer]
    public record HandFileSplittingGrainState
    {
        [Id(0)]
        public int Checkpoint { get; set; } = -1;

        [Id(1)]
        public bool IsCompleted { get; set; } = false;
    }
}
