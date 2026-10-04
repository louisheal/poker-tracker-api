using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandImport;

public interface IHandImportRepository
{
    Task<bool> TryAddHandAsync(PokerHand pokerHand);
}

public class HandImportRepository : IHandImportRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandImportRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryAddHandAsync(PokerHand pokerHand)
    {
        if (
            _dbContext.PokerHands.Local.Any(hand => hand.HandId == pokerHand.HandId)
            || await _dbContext.PokerHands.AnyAsync(hand => hand.HandId == pokerHand.HandId)
        )
        {
            return false;
        }

        var rawHandExists = _dbContext.RawHands.Local.Any(hand => hand.HandId == pokerHand.HandId);
        if (
            !rawHandExists
            && !await _dbContext.RawHands.AnyAsync(hand => hand.HandId == pokerHand.HandId)
        )
        {
            throw new InvalidOperationException(
                $"Cannot add parsed hand '{pokerHand.HandId}' because its raw hand does not exist."
            );
        }

        _dbContext.PokerHands.Add(pokerHand);
        return true;
    }
}
