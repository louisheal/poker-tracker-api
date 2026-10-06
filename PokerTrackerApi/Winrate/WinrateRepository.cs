using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Metrics;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.Winrate;

public interface IWinrateRepository
{
    Task<WinrateGraphDto> GetWinrateGraphAsync(CancellationToken cancellationToken);
}

public class WinrateRepository : IWinrateRepository
{
    private const int HandsPerPoint = 100;

    private readonly PokerTrackerDbContext _dbContext;

    public WinrateRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WinrateGraphDto> GetWinrateGraphAsync(CancellationToken cancellationToken)
    {
        var hands = _dbContext
            .PokerHands.AsNoTracking()
            .Include(hand => hand.Events)
            .AsSingleQuery()
            .OrderBy(hand => hand.Timestamp)
            .ThenBy(hand => hand.HandId)
            .AsAsyncEnumerable();

        var points = new List<WinrateHandBatchPointDto>();
        var handCount = 0;
        var net = 0m;
        var withShowdown = 0m;
        var withoutShowdown = 0m;

        await foreach (var hand in hands.WithCancellation(cancellationToken))
        {
            var result = HeroHandResultCalculator.Calculate(hand);
            handCount++;
            net += result.NetWinningsBB;
            if (result.WentToShowdown)
            {
                withShowdown += result.NetWinningsBB;
            }
            else
            {
                withoutShowdown += result.NetWinningsBB;
            }

            if (handCount % HandsPerPoint == 0)
            {
                points.Add(new WinrateHandBatchPointDto(handCount, net, withShowdown, withoutShowdown));
            }
        }

        if (handCount % HandsPerPoint != 0)
        {
            points.Add(new WinrateHandBatchPointDto(handCount, net, withShowdown, withoutShowdown));
        }

        return new WinrateGraphDto(points);
    }
}
