using PokerTrackerApi.Domain.PokerHand;

namespace PokerTrackerApi.HandImport;

public interface IHandImportRepository
{
    Task AddHand(PokerHand pokerHand);
}

public class HandImportRepository : IHandImportRepository
{
    public Task AddHand(PokerHand pokerHand)
    {
        throw new NotImplementedException();
    }
}
