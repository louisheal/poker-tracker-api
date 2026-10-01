using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistoryRepository
{
    Task<ParsedHand[]> GetParsedHands(CancellationToken cancellationToken);
}

public class HandHistoryRepository : IHandHistoryRepository
{
    private const int MaxParsedHands = 100;
    private readonly PokerTrackerDbContext _dbContext;

    public HandHistoryRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ParsedHand[]> GetParsedHands(CancellationToken cancellationToken) =>
        _dbContext.ParsedHands
            .AsNoTracking()
            .OrderBy(hand => hand.HandId)
            .Take(MaxParsedHands)
            .ToArrayAsync(cancellationToken);
}