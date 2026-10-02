using PokerTrackerApi.HandHistories;
using PokerTrackerApi.HandImport.HandReaders;
using PokerTrackerApi.HandParsing;
using PokerTrackerApi.HandReplays;
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
    private readonly IHandHistorySummaryRepository _handHistorySummaryRepository;
    private readonly IHandReplayRepository _handReplayRepository;
    private readonly IPokerHandParser _parser;
    private readonly IPokerHandReader _reader;
    private readonly ILogger<HandImportService> _logger;

    public HandImportService(
        IUnitOfWork unitOfWork,
        IRawHandRepository handHistoryRepository,
        IPreflopSpotRepository preflopRepository,
        IHandHistorySummaryRepository handHistorySummaryRepository,
        IHandReplayRepository handReplayRepository,
        IPokerHandParser parser,
        IPokerHandReader reader,
        ILogger<HandImportService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _repository = handHistoryRepository;
        _preflopRepository = preflopRepository;
        _handHistorySummaryRepository = handHistorySummaryRepository;
        _handReplayRepository = handReplayRepository;
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
                if (!await _repository.TryAddRawHandAsync(hand.HandId, rawHand, cancellationToken))
                {
                    duplicateHands++;
                    continue;
                }

                _preflopRepository.AddPreflopSpots(hand.ToPreflopSpots());
                _handHistorySummaryRepository.AddHandHistorySummary(hand.ToHandHistorySummary());
                _handReplayRepository.AddHandReplay(hand.ToHandReplay());

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                savedHands++;
            }
        }

        return new HandImportSummary(files.Count, savedHands, duplicateHands, invalidHands);
    }
}
