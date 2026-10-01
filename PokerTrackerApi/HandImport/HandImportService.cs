using PokerTrackerApi.HandImport.Parsers;
using PokerTrackerApi.HandImport.Readers;

namespace PokerTrackerApi.HandImport;

public interface IHandImportService
{
    Task<HandImportSummary> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken);
}

public class HandImportervice : IHandImportService
{
    private readonly IHandImportRepository _repository;
    private readonly IPokerHandParser _parser;
    private readonly IPokerHandReader _reader;
    private readonly ILogger<HandImportervice> _logger;

    public HandImportervice(
        IHandImportRepository handHistoryRepository,
        IPokerHandParser parser,
        IPokerHandReader reader,
        ILogger<HandImportervice> logger)
    {
        _repository = handHistoryRepository;
        _parser = parser;
        _logger = logger;
        _reader = reader;
    }

    public async Task<HandImportSummary> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var savedHands = 0;
        var duplicateHands = 0;
        var invalidHands = 0;

        foreach (var file in files)
        {
            using var reader = new StreamReader(file.OpenReadStream(), detectEncodingFromByteOrderMarks: true);

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
                    throw new InvalidOperationException($"Unexpected hand result type: {handResult.GetType().Name}");
                }

                var parseResult = _parser.ParseHand(hand.RawText);
                if (parseResult.Hand == null)
                {
                    invalidHands++;
                    _logger.LogWarning("Skipping hand {HandId}: {Reason}", hand.HandId, parseResult.Error);
                    continue;
                }

                if (await _repository.TryAddImportedHandAsync(
                    hand.HandId,
                    hand.RawText,
                    parseResult.Hand!.PreflopSpots,
                    cancellationToken))
                {
                    savedHands++;
                }
                else
                {
                    duplicateHands++;
                }
            }
        }

        return new HandImportSummary(files.Count, savedHands, duplicateHands, invalidHands);
    }
}