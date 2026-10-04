using PokerTrackerApi.HandImporting.HandParsers;
using PokerTrackerApi.HandImporting.HandReaders;

namespace PokerTrackerApi.HandImporting;

public interface IHandImportService
{
    Task<HandImportSummary> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken
    );
}

public class HandImportService : IHandImportService
{
    private readonly IHandImportRepository _repository;
    private readonly IPokerHandParser _parser;
    private readonly IPokerHandReader _reader;
    private readonly ILogger<HandImportService> _logger;

    public HandImportService(
        IHandImportRepository handImportRepository,
        IPokerHandParser parser,
        IPokerHandReader reader,
        ILogger<HandImportService> logger
    )
    {
        _repository = handImportRepository;
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

                savedHands++;
            }
        }

        return new HandImportSummary(files.Count, savedHands, duplicateHands, invalidHands);
    }
}
