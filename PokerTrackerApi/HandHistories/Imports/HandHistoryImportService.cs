using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using PokerTrackerApi.HandHistories;

namespace PokerTrackerApi.HandHistories.Imports;

public class HandHistoryImportService : IHandHistoryImportService
{
    private readonly IHandHistoryRepository _handHistoryRepository;

    public HandHistoryImportService(IHandHistoryRepository handHistoryRepository)
    {
        _handHistoryRepository = handHistoryRepository;
    }

    private static readonly Regex HandStartRegex = new("(?m)^Poker Hand #", RegexOptions.Compiled);
    private static readonly Regex HandIdRegex = new("\\APoker Hand #(?<id>RC[0-9]+):", RegexOptions.Compiled);

    public async Task<HandHistoryImportSummary> ImportAsync(
        IReadOnlyCollection<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var savedHands = 0;
        var duplicateHands = 0;
        var invalidHands = 0;

        foreach (var file in files)
        {
            using var reader = new StreamReader(file.OpenReadStream(), detectEncodingFromByteOrderMarks: true);
            var contents = await reader.ReadToEndAsync(cancellationToken);

            foreach (var rawText in SplitHands(contents))
            {
                var idMatch = HandIdRegex.Match(rawText);
                if (!idMatch.Success)
                {
                    invalidHands++;
                    continue;
                }

                var handId = idMatch.Groups["id"].Value;
                if (await _handHistoryRepository.TryAddImportedHandAsync(handId, rawText, cancellationToken))
                {
                    savedHands++;
                }
                else
                {
                    duplicateHands++;
                }
            }
        }

        return new HandHistoryImportSummary(files.Count, savedHands, duplicateHands, invalidHands);
    }

    private static List<string> SplitHands(string contents)
    {
        var headers = HandStartRegex.Matches(contents);
        var hands = new List<string>(headers.Count);

        for (var index = 0; index < headers.Count; index++)
        {
            var start = headers[index].Index;
            var end = index + 1 < headers.Count ? headers[index + 1].Index : contents.Length;
            var rawText = contents[start..end].TrimEnd('\r', '\n');
            if (rawText.Length > 0)
            {
                hands.Add(rawText);
            }
        }

        return hands;
    }
}