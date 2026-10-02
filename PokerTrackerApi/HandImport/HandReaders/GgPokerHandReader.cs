using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace PokerTrackerApi.HandImport.HandReaders;

public class GgPokerHandReader : IPokerHandReader
{
    public GgPokerHandReader() { }

    public async IAsyncEnumerable<string> ReadHandsAsync(
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
                    yield return currentHand.ToString();
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
            yield return currentHand.ToString();
        }
    }
}
