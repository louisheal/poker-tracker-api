using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistorySummaryRepository
{
    void AddHandHistorySummary(HandHistorySummary summary);
    Task UpsertHandHistorySummary(HandHistorySummary summary, CancellationToken cancellationToken);
    Task<HandHistorySummary[]> GetHandHistorySummaries(CancellationToken cancellationToken);
}

public class HandHistorySummaryRepository : IHandHistorySummaryRepository
{
    private const int MaxHandHistorySummaries = 100;
    private readonly PokerTrackerDbContext _dbContext;

    public HandHistorySummaryRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void AddHandHistorySummary(HandHistorySummary summary) =>
        _dbContext.HandHistorySummaries.Add(summary);

    public async Task UpsertHandHistorySummary(
        HandHistorySummary summary,
        CancellationToken cancellationToken
    )
    {
        var handHistorySummary = await _dbContext.HandHistorySummaries.SingleOrDefaultAsync(
            hand => hand.HandId == summary.HandId,
            cancellationToken
        );

        if (handHistorySummary is null)
        {
            AddHandHistorySummary(summary);
            return;
        }

        handHistorySummary.HoleCards = summary.HoleCards;
        handHistorySummary.HeroPosition = summary.HeroPosition;
    }

    public Task<HandHistorySummary[]> GetHandHistorySummaries(
        CancellationToken cancellationToken
    ) =>
        _dbContext
            .HandHistorySummaries.AsNoTracking()
            .OrderBy(hand => hand.HandId)
            .Take(MaxHandHistorySummaries)
            .ToArrayAsync(cancellationToken);
}
