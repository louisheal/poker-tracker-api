using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistorySummaryRepository
{
    void AddHandHistorySummary(string handId, HoleCards holeCards, PokerPosition heroPosition);
    Task UpsertHandHistorySummary(
        string handId,
        HoleCards holeCards,
        PokerPosition heroPosition,
        CancellationToken cancellationToken
    );
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

    public void AddHandHistorySummary(
        string handId,
        HoleCards holeCards,
        PokerPosition heroPosition
    )
    {
        _dbContext.HandHistorySummaries.Add(
            new HandHistorySummary
            {
                HandId = handId,
                HoleCards = holeCards,
                HeroPosition = heroPosition,
            }
        );
    }

    public async Task UpsertHandHistorySummary(
        string handId,
        HoleCards holeCards,
        PokerPosition heroPosition,
        CancellationToken cancellationToken
    )
    {
        var handHistorySummary = await _dbContext.HandHistorySummaries.SingleOrDefaultAsync(
            hand => hand.HandId == handId,
            cancellationToken
        );

        if (handHistorySummary is null)
        {
            AddHandHistorySummary(handId, holeCards, heroPosition);
            return;
        }

        handHistorySummary.HoleCards = holeCards;
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
