using PokerTrackerApi.HandImports;
using PokerTrackerApi.HandParsing;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandReprocessing;

public interface IHandReprocessingService
{
    Task ReprocessRawHands(CancellationToken cancellationToken);
}

public class HandReprocessingService : IHandReprocessingService
{
    private readonly IPokerHandParser _parser;
    private readonly IRawHandRepository _rawHandRepository;
    private readonly IPreflopSpotRepository _preflopSpotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IHandReprocessingService> _logger;

    public HandReprocessingService(
        IPokerHandParser parser,
        IRawHandRepository rawHandRepository,
        IPreflopSpotRepository preflopSpotRepository,
        IUnitOfWork unitOfWork,
        ILogger<IHandReprocessingService> logger
    )
    {
        _parser = parser;
        _rawHandRepository = rawHandRepository;
        _preflopSpotRepository = preflopSpotRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ReprocessRawHands(CancellationToken cancellationToken)
    {
        await foreach (var batch in _rawHandRepository.GetRawHandsBatchedAsync(cancellationToken))
        {
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

                await _preflopSpotRepository.ReplacePreflopSpots(
                    hand.HandId,
                    hand.ToPreflopSpots(),
                    cancellationToken
                );
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _unitOfWork.ClearTracking();
        }
    }
}
