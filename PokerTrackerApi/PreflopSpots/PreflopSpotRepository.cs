using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.PreflopSpots;

public interface IPreflopSpotRepository
{
    Task<RangeActionsDto> GetRangeAsync(string spotKey, CancellationToken cancellationToken);
}

public class PreflopSpotRepository : IPreflopSpotRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public PreflopSpotRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // TODO : does this even need to return a boolean?
    // does any code path return false?
    public async Task<bool> TryAddHandAsync(
        // TODO : should this be processed outside this repository?
        // so that it only speaks in terms of Dao/EF classes?
        PokerHand pokerHand,
        CancellationToken cancellationToken
    )
    {
        _dbContext.PreflopSpots.AddRange(pokerHand.ToPreflopSpots());
        // TODO : didn't we have something like an IUnitOfWork to handle this?
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

    // TODO : should a repository be returning a Dto?
    // guess it's fine because this is a super thin call
    public async Task<RangeActionsDto> GetRangeAsync(
        string spotKey,
        CancellationToken cancellationToken
    )
    {
        var actionCounts = await _dbContext
            .PreflopSpots.AsNoTracking()
            .Where(spot => spot.SpotKey == spotKey && spot.IsHero && spot.HandKey != null)
            .GroupBy(spot => new { spot.HandKey, spot.Action })
            .Select(group => new
            {
                group.Key.HandKey,
                group.Key.Action,
                Count = group.Count(),
            })
            .ToListAsync(cancellationToken);

        var hands = actionCounts
            .GroupBy(count => count.HandKey)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var total = group.Sum(count => count.Count);
                var countsByAction = group.ToDictionary(
                    count => count.Action,
                    count => count.Count
                );
                return new HandActionsDto(
                    group.Key!,
                    Percentage(countsByAction, PokerAction.Fold, total),
                    Percentage(countsByAction, PokerAction.Call, total),
                    Percentage(countsByAction, PokerAction.Raise, total)
                );
            })
            .ToArray();

        return new RangeActionsDto(spotKey, hands);
    }

    private static double Percentage(
        IReadOnlyDictionary<PokerAction, int> counts,
        PokerAction action,
        int total
    )
    {
        return counts.TryGetValue(action, out var count) ? (double)count / total : 0;
    }
}
