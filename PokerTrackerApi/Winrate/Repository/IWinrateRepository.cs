namespace PokerTrackerApi.Winrate.Repository;

public interface IWinrateRepository
{
    Task SaveWinrate(Winrate winrate, string handId, CancellationToken cancellationToken);
    IAsyncEnumerable<Winrate> GetWinrates(CancellationToken cancellationToken);
}
