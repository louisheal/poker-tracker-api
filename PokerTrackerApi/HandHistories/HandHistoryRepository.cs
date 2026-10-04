using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistoryRepository
{
    Task<HandHistory[]> GetHandHistoriesAsync(CancellationToken cancellationToken);
}

public class HandHistoryRepository : IHandHistoryRepository
{
    private const int MaxHandHistorySummaries = 100;
    private readonly PokerTrackerDbContext _dbContext;

    public HandHistoryRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HandHistory[]> GetHandHistoriesAsync(CancellationToken cancellationToken)
    {
        var pokerHands = await _dbContext
            .PokerHands.AsNoTracking()
            .OrderBy(hand => hand.HandId)
            .Take(MaxHandHistorySummaries)
            .ToArrayAsync(cancellationToken);

        return pokerHands.Select(ToHandHistory).ToArray();
    }

    private static HandHistory ToHandHistory(PokerHand hand) =>
        new(hand.HandId, hand.HeroHoleCards);
}
