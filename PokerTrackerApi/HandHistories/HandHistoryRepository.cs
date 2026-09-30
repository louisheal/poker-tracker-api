using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IHandHistoryRepository
{
    Task<bool> TryAddImportedHandAsync(string handId, string rawText, CancellationToken cancellationToken);
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

    public async Task<bool> TryAddImportedHandAsync(string handId, string rawText, CancellationToken cancellationToken)
    {
        if (await _dbContext.RawHands.AnyAsync(hand => hand.HandId == handId, cancellationToken))
        {
            return false;
        }

        _dbContext.RawHands.Add(new RawHand { HandId = handId, RawText = rawText });
        _dbContext.ParsedHands.Add(new ParsedHand { HandId = handId });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<ParsedHand[]> GetParsedHands(CancellationToken cancellationToken) =>
        _dbContext.ParsedHands
            .AsNoTracking()
            .OrderBy(hand => hand.HandId)
            .Take(MaxParsedHands)
            .ToArrayAsync(cancellationToken);
}