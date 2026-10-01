using PokerTrackerApi.HandHistories;
using PokerTrackerApi.HandImport;
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
    private readonly IParsedHandRepository _parsedHandRepository;
    private readonly IPreflopSpotRepository _preflopSpotRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<IHandReprocessingService> _logger;

    public HandReprocessingService(
        IPokerHandParser parser,
        IRawHandRepository rawHandRepository,
        IParsedHandRepository parsedHandRepository,
        IPreflopSpotRepository preflopSpotRepository,
        IUnitOfWork unitOfWork,
        ILogger<IHandReprocessingService> logger)
    {
        _parser = parser;
        _rawHandRepository = rawHandRepository;
        _parsedHandRepository = parsedHandRepository;
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
                if (parseResult.Hand == null)
                {
                    _logger.LogWarning("Skipping hand {HandId}: {Reason}", rawHand.HandId, parseResult.Error);
                    continue;
                }

                _parsedHandRepository.AddParsedHand(rawHand.HandId, parseResult.Hand.HoleCards);
                _preflopSpotRepository.AddPreflopSpots(rawHand.HandId, parseResult.Hand.PreflopSpots);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}