using PokerTrackerApi.HandHistories;
using PokerTrackerApi.HandImport.HandReaders;
using PokerTrackerApi.HandParsing;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandImport;

public interface IHandImportService
{
    Task<HandImportSummary> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken
    );
}

public class HandImportService : IHandImportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRawHandRepository _repository;
    private readonly IPreflopSpotRepository _preflopRepository;
    private readonly IParsedHandRepository _parsedHandRepository;
    private readonly IPokerHandParser _parser;
    private readonly IPokerHandReader _reader;
    private readonly ILogger<HandImportService> _logger;

    public HandImportService(
        IUnitOfWork unitOfWork,
        IRawHandRepository handHistoryRepository,
        IPreflopSpotRepository preflopRepository,
        IParsedHandRepository parsedHandRepository,
        IPokerHandParser parser,
        IPokerHandReader reader,
        ILogger<HandImportService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _repository = handHistoryRepository;
        _preflopRepository = preflopRepository;
        _parsedHandRepository = parsedHandRepository;
        _parser = parser;
        _logger = logger;
        _reader = reader;
    }

    public async Task<HandImportSummary> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        var savedHands = 0;
        var duplicateHands = 0;
        var invalidHands = 0;

        foreach (var file in files)
        {
            using var reader = new StreamReader(
                file.OpenReadStream(),
                detectEncodingFromByteOrderMarks: true
            );

            await foreach (var handResult in _reader.ReadHandsAsync(reader, cancellationToken))
            {
                if (handResult is HandReadFailure failure)
                {
                    invalidHands++;
                    _logger.LogWarning("Skipping hand: {Reason}", failure.Error);
                    continue;
                }

                if (handResult is not HandReadSuccess hand)
                {
                    throw new InvalidOperationException(
                        $"Unexpected hand result type: {handResult.GetType().Name}"
                    );
                }

                var parseResult = _parser.ParseHand(hand.RawText);
                if (parseResult.Hand == null)
                {
                    invalidHands++;
                    _logger.LogWarning(
                        "Skipping hand {HandId}: {Reason}",
                        hand.HandId,
                        parseResult.Error
                    );
                    continue;
                }

                if (
                    !await _repository.TryAddRawHandAsync(
                        hand.HandId,
                        hand.RawText,
                        cancellationToken
                    )
                )
                {
                    duplicateHands++;
                    continue;
                }

                _preflopRepository.AddPreflopSpots(hand.HandId, parseResult.Hand.PreflopSpots);
                _parsedHandRepository.AddParsedHand(hand.HandId, parseResult.Hand.HoleCards);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                savedHands++;
            }
        }

        return new HandImportSummary(files.Count, savedHands, duplicateHands, invalidHands);
    }
}
