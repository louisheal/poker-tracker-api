using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PokerTrackerApi.HandImport.HandReaders;

public class GgPokerHandReader : IPokerHandReader
{
    private static readonly Regex HandIdRegex = new(
        "\\APoker Hand #(?<id>RC[0-9]+):",
        RegexOptions.Compiled
    );

    public async IAsyncEnumerable<HandReadResult> ReadHandsAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        StringBuilder? currentHand = null;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("Poker Hand #", StringComparison.Ordinal))
            {
                if (currentHand is not null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return CreateResult(currentHand.ToString());
                }

                currentHand = new StringBuilder();
            }

            if (currentHand is not null)
            {
                if (currentHand.Length > 0)
                {
                    currentHand.AppendLine();
                }
                currentHand.Append(line);
            }
        }

        if (currentHand is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return CreateResult(currentHand.ToString());
        }
    }

    private static HandReadResult CreateResult(string rawText)
    {
        if (!TryParseHandId(rawText, out var handId))
        {
            return new HandReadFailure(rawText, "Failed to parse hand id");
        }
        return new HandReadSuccess(handId, rawText);
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
