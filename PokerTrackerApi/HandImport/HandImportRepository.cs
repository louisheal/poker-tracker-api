using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.HandHistories;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandImport;

public interface IRawHandRepository
{
    Task<bool> TryAddRawHandAsync(string handId, string rawText, CancellationToken cancellationToken);
}

public class RawHandRepository : IRawHandRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public RawHandRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryAddRawHandAsync(string handId, string rawText, CancellationToken cancellationToken)
    {
        if (await _dbContext.RawHands.AnyAsync(hand => hand.HandId == handId, cancellationToken))
        {
            return false;
        }

        _dbContext.RawHands.Add(new RawHand { HandId = handId, RawText = rawText });
        return true;
    }
}