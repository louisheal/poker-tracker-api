using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.HandHistories;

public interface IParsedHandRepository
{
    void AddParsedHand(string handId, HoleCards holeCards);
    Task UpsertParsedHand(string handId, HoleCards holeCards, CancellationToken cancellationToken);
    Task<ParsedHand[]> GetParsedHands(CancellationToken cancellationToken);
}

public class ParsedHandRepository : IParsedHandRepository
{
    private const int MaxParsedHands = 100;
    private readonly PokerTrackerDbContext _dbContext;

    public ParsedHandRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void AddParsedHand(string handId, HoleCards holeCards)
    {
        _dbContext.ParsedHands.Add(
            new ParsedHand
            {
                HandId = handId,
                HoleCards = holeCards,
                ButtonSeat = 0,
            }
        );
    }

    public async Task UpsertParsedHand(
        string handId,
        HoleCards holeCards,
        CancellationToken cancellationToken
    )
    {
        var parsedHand = await _dbContext.ParsedHands.SingleOrDefaultAsync(
            hand => hand.HandId == handId,
            cancellationToken
        );

        if (parsedHand is null)
        {
            AddParsedHand(handId, holeCards);
            return;
        }

        parsedHand.HoleCards = holeCards;
    }

    public Task<ParsedHand[]> GetParsedHands(CancellationToken cancellationToken) =>
        _dbContext
            .ParsedHands.AsNoTracking()
            .OrderBy(hand => hand.HandId)
            .Take(MaxParsedHands)
            .ToArrayAsync(cancellationToken);
}
