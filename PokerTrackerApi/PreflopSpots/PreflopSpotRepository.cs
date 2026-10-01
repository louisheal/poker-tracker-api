using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.HandParsing;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.PreflopSpots;

public interface IPreflopSpotRepository
{
    void AddPreflopSpots(string handId, IReadOnlyList<PreflopSpotObservation> observations);
    Task ReplacePreflopSpots(string handId, IReadOnlyList<PreflopSpotObservation> observations, CancellationToken cancellationToken);
    Task<RangeActionsDto> GetRangeAsync(string spotKey, CancellationToken cancellationToken);
}

public class PreflopSpotRepository : IPreflopSpotRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public PreflopSpotRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void AddPreflopSpots(string handId, IReadOnlyList<PreflopSpotObservation> observations)
    {
        _dbContext.PreflopSpots.AddRange(observations.Select(observation => new PreflopSpot
        {
            HandId = handId,
            SpotKey = observation.SpotKey,
            HandKey = observation.HandKey,
            Action = observation.Action,
        }));
    }

    public async Task ReplacePreflopSpots(string handId, IReadOnlyList<PreflopSpotObservation> observations, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.PreflopSpots
            .Where(spot => spot.HandId == handId)
            .ToListAsync(cancellationToken);

        _dbContext.PreflopSpots.RemoveRange(existing);

        _dbContext.PreflopSpots.AddRange(observations.Select(observation => new PreflopSpot
        {
            HandId = handId,
            SpotKey = observation.SpotKey,
            HandKey = observation.HandKey,
            Action = observation.Action,
        }));
    }

    public async Task<RangeActionsDto> GetRangeAsync(string spotKey, CancellationToken cancellationToken)
    {
        var actionCounts = await _dbContext.PreflopSpots
            .AsNoTracking()
            .Where(spot => spot.SpotKey == spotKey)
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
                var countsByAction = group.ToDictionary(count => count.Action, count => count.Count);
                return new HandActionsDto(
                    group.Key,
                    Percentage(countsByAction, PokerAction.Fold, total),
                    Percentage(countsByAction, PokerAction.Call, total),
                    Percentage(countsByAction, PokerAction.Raise, total));
            })
            .ToArray();

        // TODO : do we need to return the spotKey here?
        return new RangeActionsDto(spotKey, hands);
    }

    private static double Percentage(
        IReadOnlyDictionary<PokerAction, int> counts,
        PokerAction action,
        int total)
    {
        return counts.TryGetValue(action, out var count) ? (double)count / total : 0;
    }
}