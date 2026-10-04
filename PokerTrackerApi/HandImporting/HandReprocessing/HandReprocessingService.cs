using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.HandImporting.HandParsers;

namespace PokerTrackerApi.HandImporting.HandReprocessing;

public interface IHandReprocessingService
{
    Task ReprocessRawHands(CancellationToken cancellationToken);
}

public class HandReprocessingService : IHandReprocessingService
{
    private readonly IPokerHandParser _parser;
    private readonly IHandReprocessingRepository _reprocessingRepository;
    private readonly ILogger<IHandReprocessingService> _logger;

    public HandReprocessingService(
        IPokerHandParser parser,
        IHandReprocessingRepository reprocessingRepository,
        ILogger<IHandReprocessingService> logger
    )
    {
        _parser = parser;
        _reprocessingRepository = reprocessingRepository;
        _logger = logger;
    }

    public async Task ReprocessRawHands(CancellationToken cancellationToken)
    {
        await foreach (
            var batch in _reprocessingRepository.GetRawHandsBatchedAsync(cancellationToken)
        )
        {
            var parsedHands = new List<PokerHand>(batch.Count);
            foreach (var rawHand in batch)
            {
                var parseResult = _parser.ParseHand(rawHand.RawText);
                if (parseResult is HandHistoryParseFailure failure)
                {
                    _logger.LogWarning(
                        "Skipping hand {HandId}: {Reason}",
                        rawHand.HandId,
                        failure.Error
                    );
                    continue;
                }

                if (parseResult is not HandHistoryParseSuccess success)
                {
                    throw new InvalidOperationException(
                        $"Unexpected parse result type: {parseResult.GetType().Name}"
                    );
                }

                var hand =
                    success.Hand
                    ?? throw new InvalidOperationException(
                        "A successful parse result must contain a parsed hand."
                    );
                if (!string.Equals(hand.HandId, rawHand.HandId, StringComparison.Ordinal))
                {
                    _logger.LogError(
                        "Stored hand id {StoredHandId} does not match parsed hand id {ParsedHandId}",
                        rawHand.HandId,
                        hand.HandId
                    );
                    continue;
                }

                parsedHands.Add(hand);
            }

            await _reprocessingRepository.UpsertPokerHandsAsync(parsedHands, cancellationToken);
        }
    }
}
