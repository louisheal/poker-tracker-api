using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PokerTrackerApi.HandFileSplitting;

public partial class GgPokerHandFileSplitter : IHandFileSplitter
{
    [GeneratedRegex(@"^Poker Hand #(?<id>[^:]+):.* - (?<timestamp>[0-9]{4}/[0-9]{2}/[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2})$")]
    private static partial Regex HandHeaderRegex();

    public async IAsyncEnumerable<RawHand> SplitHandsAsync(
        Stream handFile,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(
            handFile,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true
        );

        StringBuilder? rawText = null;
        string? handId = null;
        int sequence = 0;

        await foreach (var line in ReadLinesAsync(reader, cancellationToken))
        {
            var handHeader = HandHeaderRegex().Match(line);
            if (handHeader.Success)
            {
                if (rawText != null)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new RawHand(handId!, rawText.ToString(), sequence++);
                }

                handId = handHeader.Groups["id"].Value;
                rawText = new StringBuilder();
            }

            if (rawText != null)
            {
                if (rawText.Length > 0)
                {
                    rawText.AppendLine();
                }
                rawText.Append(line);
            }
        }

        if (rawText != null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new RawHand(handId!, rawText.ToString(), sequence++);
        }
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(
        TextReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        for (
            var line = await reader.ReadLineAsync(cancellationToken);
            line != null;
            line = await reader.ReadLineAsync(cancellationToken)
        )
        {
            yield return line;
        }
    }
}
