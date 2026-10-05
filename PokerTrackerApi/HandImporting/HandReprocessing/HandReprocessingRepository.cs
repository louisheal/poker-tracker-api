using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.HandNotes;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandImporting.HandReprocessing;

public interface IHandReprocessingRepository
{
    IAsyncEnumerable<IReadOnlyList<RawHand>> GetRawHandsBatchedAsync(
        CancellationToken cancellationToken
    );
    Task UpsertPokerHandsAsync(
        IReadOnlyList<PokerHand> pokerHands,
        CancellationToken cancellationToken
    );
}

public class HandReprocessingRepository : IHandReprocessingRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandReprocessingRepository(PokerTrackerDbContext dbContext)
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

    public async Task UpsertPokerHandsAsync(
        IReadOnlyList<PokerHand> pokerHands,
        CancellationToken cancellationToken
    )
    {
        if (pokerHands.Count == 0)
        {
            return;
        }

        var handIds = pokerHands.Select(hand => hand.HandId).ToArray();
        var preflopSpots = pokerHands.SelectMany(hand => hand.ToPreflopSpots()).ToArray();

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            cancellationToken
        );
        try
        {
            var existingLabels = await _dbContext
                .HandLabelAssignments.AsNoTracking()
                .Where(assignment => Enumerable.Contains(handIds, assignment.HandId))
                .ToArrayAsync(cancellationToken);
            await _dbContext
                .PreflopSpots.Where(spot => Enumerable.Contains(handIds, spot.HandId))
                .ExecuteDeleteAsync(cancellationToken);
            await _dbContext
                .PokerHands.Where(hand => Enumerable.Contains(handIds, hand.HandId))
                .ExecuteDeleteAsync(cancellationToken);

            _dbContext.PokerHands.AddRange(pokerHands);
            _dbContext.PreflopSpots.AddRange(preflopSpots);
            _dbContext.HandLabelAssignments.AddRange(
                existingLabels.Select(assignment => new HandLabelAssignment
                {
                    HandId = assignment.HandId,
                    Street = assignment.Street,
                    Label = assignment.Label,
                })
            );
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }
    }
}
