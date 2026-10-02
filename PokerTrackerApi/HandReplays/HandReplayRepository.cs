using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandReplays;

public interface IHandReplayRepository
{
    Task<HandReplay?> GetHandReplayAsync(string handId, CancellationToken cancellationToken);
    void AddHandReplay(HandReplay replay);
    Task UpsertHandReplay(HandReplay replay, CancellationToken cancellationToken);
}

public class HandReplayRepository : IHandReplayRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public HandReplayRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<HandReplay?> GetHandReplayAsync(
        string handId,
        CancellationToken cancellationToken
    ) =>
        _dbContext
            .HandReplays.AsNoTracking()
            .Include(replay => replay.Players)
            .Include(replay => replay.Events)
                .ThenInclude(handEvent => handEvent.Cards)
            .SingleOrDefaultAsync(replay => replay.HandId == handId, cancellationToken);

    public void AddHandReplay(HandReplay replay) => _dbContext.HandReplays.Add(replay);

    public async Task UpsertHandReplay(HandReplay replay, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.HandReplays.SingleOrDefaultAsync(
            handReplay => handReplay.HandId == replay.HandId,
            cancellationToken
        );

        if (existing is null)
        {
            AddHandReplay(replay);
            return;
        }

        existing.HeroHoleCards = replay.HeroHoleCards;
        existing.HeroPosition = replay.HeroPosition;

        var existingPlayers = await _dbContext
            .HandReplayPlayers.Where(player => player.HandId == replay.HandId)
            .ToListAsync(cancellationToken);
        _dbContext.HandReplayPlayers.RemoveRange(existingPlayers);

        var existingEvents = await _dbContext
            .HandReplayEvents.Include(handEvent => handEvent.Cards)
            .Where(handEvent => handEvent.HandId == replay.HandId)
            .ToListAsync(cancellationToken);
        _dbContext.HandReplayEvents.RemoveRange(existingEvents);

        _dbContext.HandReplayPlayers.AddRange(replay.Players);
        _dbContext.HandReplayEvents.AddRange(replay.Events);
    }
}
