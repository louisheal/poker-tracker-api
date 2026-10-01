using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace PokerTrackerApi.HandImport.Readers;

public class GgPokerHandReader : IPokerHandReader
{
    private static readonly Regex HandStartRegex = new("(?m)^Poker Hand #", RegexOptions.Compiled);
    private static readonly Regex HandIdRegex = new("\\APoker Hand #(?<id>RC[0-9]+):", RegexOptions.Compiled);

    public async IAsyncEnumerable<HandReadResult> ReadHandsAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var contents = await reader.ReadToEndAsync(cancellationToken);

        var headers = HandStartRegex.Matches(contents);
        for (var index = 0; index < headers.Count; index++)
        {
            var start = headers[index].Index;
            var end = index + 1 < headers.Count ? headers[index + 1].Index : contents.Length;
            var rawText = contents[start..end].TrimEnd('\r', '\n');
            if (rawText.Length > 0)
            {
                if (!TryParseHandId(rawText, out var handId))
                {
                    yield return new HandReadFailure(rawText, "Failed to parse hand id");
                    continue;
                }
                yield return new HandReadSuccess(handId, rawText);
            }
        }
    }

    private static bool TryParseHandId(string rawText, [NotNullWhen(true)] out string? handId)
    {
        var idMatch = HandIdRegex.Match(rawText);
        if (!idMatch.Success)
        {
            handId = null;
            return false;
        }
        handId = idMatch.Groups["id"].Value;
        return true;
    }
}