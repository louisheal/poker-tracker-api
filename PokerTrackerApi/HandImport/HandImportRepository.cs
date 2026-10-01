using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.HandHistories;
using PokerTrackerApi.Persistence;
using PokerTrackerApi.PreflopSpots;

namespace PokerTrackerApi.HandImport;

public interface IHandImportRepository
{
    Task<bool> TryAddImportedHandAsync(
        string handId,
        string rawText,
        HoleCards HoleCards,
        IReadOnlyList<Parsers.PreflopSpot> observations,
        CancellationToken cancellationToken);
}

public class HandImportRepository : IHandImportRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandImportRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> TryAddImportedHandAsync(
        string handId,
        string rawText,
        HoleCards HoleCards,
        IReadOnlyList<Parsers.PreflopSpot> observations,
        CancellationToken cancellationToken)
    {
        if (await _dbContext.RawHands.AnyAsync(hand => hand.HandId == handId, cancellationToken))
        {
            return false;
        }

        _dbContext.RawHands.Add(new RawHand { HandId = handId, RawText = rawText });
        _dbContext.ParsedHands.Add(new ParsedHand { HandId = handId, HoleCards = HoleCards });
        _dbContext.PreflopSpots.AddRange(observations.Select(observation => new PreflopSpot
        {
            HandId = handId,
            SpotKey = observation.SpotKey,
            HandKey = observation.HandKey,
            Action = observation.Action,
        }));
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}