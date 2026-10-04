using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandImports;

public interface IRawHandRepository
{
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
