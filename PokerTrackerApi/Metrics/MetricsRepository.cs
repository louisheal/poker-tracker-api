using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.Metrics;

public interface IMetricsRepository
{
    Task<MetricsDto> GetMetricsAsync(CancellationToken cancellationToken);
}

public class MetricsRepository : IMetricsRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public MetricsRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MetricsDto> GetMetricsAsync(CancellationToken cancellationToken)
    {
        var hands = _dbContext
            .PokerHands.AsNoTracking()
            .Include(hand => hand.Events)
            .AsSingleQuery()
            .AsAsyncEnumerable();

        var handsPlayed = 0;
        var netWinningsBB = 0m;
        var flopsSeen = 0;
        var showdowns = 0;
        var showdownsWon = 0;

        await foreach (var hand in hands.WithCancellation(cancellationToken))
        {
            var result = HeroHandResultCalculator.Calculate(hand);
            handsPlayed++;
            netWinningsBB += result.NetWinningsBB;
            if (result.SawFlop)
            {
                flopsSeen++;
            }
            if (result.WentToShowdown)
            {
                showdowns++;
            }
            if (result.WonAtShowdown)
            {
                showdownsWon++;
            }
        }

        return new MetricsDto(
            handsPlayed,
            handsPlayed == 0 ? 0m : netWinningsBB / handsPlayed * 100m,
            flopsSeen == 0 ? 0m : (decimal)showdowns / flopsSeen * 100m,
            showdowns == 0 ? 0m : (decimal)showdownsWon / showdowns * 100m
        );
    }
}
