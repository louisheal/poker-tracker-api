using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandImport;

public interface IHandImportRepository
{
    Task<bool> TryAddHandAsync(PokerHand pokerHand);
}

public class HandImportRepository : IHandImportRepository
{
    public readonly PokerTrackerDbContext _dbContext;

    public HandImportRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> TryAddHandAsync(PokerHand pokerHand)
    {
        throw new NotImplementedException();
    }
}
