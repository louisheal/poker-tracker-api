using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandImport;

public interface IRawHandRepository
{
    Task<bool> TryAddRawHandAsync(
        string handId,
        string rawText,
        CancellationToken cancellationToken
    );
    IAsyncEnumerable<IReadOnlyList<RawHand>> GetRawHandsBatchedAsync(
        CancellationToken cancellationToken
    );
}

public class RawHandRepository : IRawHandRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public RawHandRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryAddRawHandAsync(
        string handId,
        string rawText,
        CancellationToken cancellationToken
    )
    {
        if (await _dbContext.RawHands.AnyAsync(hand => hand.HandId == handId, cancellationToken))
        {
            return false;
        }

        _dbContext.RawHands.Add(new RawHand { HandId = handId, RawText = rawText });
        return true;
    }

    public async IAsyncEnumerable<IReadOnlyList<RawHand>> GetRawHandsBatchedAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        const int batchSize = 100;
        var offset = 0;

        while (true)
        {
            var batch = await _dbContext
                .RawHands.AsNoTracking()
                .OrderBy(hand => hand.HandId)
                .Skip(offset)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                yield break;
            }

            yield return batch;
            offset += batch.Count;
        }
    }
}
