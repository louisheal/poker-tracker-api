using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public sealed class HandHistoryRepository(PokerTrackerDbContext dbContext)
{
    public async Task<bool> TryAddImportedHandAsync(string handId, string rawText, CancellationToken cancellationToken)
    {
        if (await dbContext.RawHands.AnyAsync(hand => hand.HandId == handId, cancellationToken))
        {
            return false;
        }

        dbContext.RawHands.Add(new RawHand { HandId = handId, RawText = rawText });
        dbContext.ParsedHands.Add(new ParsedHand { HandId = handId });
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}