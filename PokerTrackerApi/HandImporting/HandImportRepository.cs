using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.MassData;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandImporting;

public interface IHandImportRepository
{
    Task<bool> TryAddHandAsync(
        PokerHand pokerHand,
        string rawText,
        CancellationToken cancellationToken
    );
}

public class HandImportRepository : IHandImportRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandImportRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryAddHandAsync(
        PokerHand pokerHand,
        string rawText,
        CancellationToken cancellationToken
    )
    {
        if (
            _dbContext.RawHands.Local.Any(hand => hand.HandId == pokerHand.HandId)
            || await _dbContext.RawHands.AnyAsync(
                hand => hand.HandId == pokerHand.HandId,
                cancellationToken
            )
        )
        {
            return false;
        }

        _dbContext.RawHands.Add(new RawHand { HandId = pokerHand.HandId, RawText = rawText });
        _dbContext.PokerHands.Add(pokerHand);
        _dbContext.PreflopSpots.AddRange(pokerHand.ToPreflopSpots());
        _dbContext.PostflopBettingSpots.AddRange(PostflopBettingSpotExtractor.Extract(pokerHand));
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _dbContext.ChangeTracker.Clear();
        }

        return true;
    }
}
