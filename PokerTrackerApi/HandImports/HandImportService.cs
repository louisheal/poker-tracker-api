using PokerTrackerApi.HandImport.HandReaders;
using PokerTrackerApi.HandParsing;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandImports;

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
    private readonly IHandImportRepository _repository;
    private readonly IPreflopSpotRepository _preflopSpotRepository;
    private readonly IPokerHandParser _parser;
    private readonly IPokerHandReader _reader;
    private readonly ILogger<HandImportService> _logger;

    public HandImportService(
        IUnitOfWork unitOfWork,
        IHandImportRepository handImportRepository,
        IPreflopSpotRepository preflopSpotRepository,
        IPokerHandParser parser,
        IPokerHandReader reader,
        ILogger<HandImportService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _repository = handImportRepository;
        _preflopSpotRepository = preflopSpotRepository;
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

            await foreach (var rawHand in _reader.ReadHandsAsync(reader, cancellationToken))
            {
                var parseResult = _parser.ParseHand(rawHand);
                if (parseResult is HandHistoryParseFailure failure)
                {
                    invalidHands++;
                    _logger.LogWarning("Skipping hand: {Reason}", failure.Error);
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
                if (!await _repository.TryAddHandAsync(hand, rawHand, cancellationToken))
                {
                    duplicateHands++;
                    continue;
                }

                _preflopSpotRepository.AddPreflopSpots(hand.ToPreflopSpots());
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                savedHands++;
            }
        }

        return new HandImportSummary(files.Count, savedHands, duplicateHands, invalidHands);
    }
}
