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
    private const int MovingAverageWindowPoints = 10;

    private readonly PokerTrackerDbContext _dbContext;

    public WinrateRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WinrateGraphDto> GetWinrateGraphAsync(CancellationToken cancellationToken)
    {
        var newestHands = _dbContext
            .PokerHands.AsNoTracking()
            .Include(hand => hand.Events)
            .OrderByDescending(hand => hand.Timestamp)
            .ThenByDescending(hand => hand.HandId)
            .Take(10_000);

        var hands = newestHands
            .OrderBy(hand => hand.Timestamp)
            .ThenBy(hand => hand.HandId)
            .AsSingleQuery()
            .AsAsyncEnumerable();

        var points = new List<WinrateHandBatchPointDto>();
        var handCount = 0;
        var net = 0m;
        var withShowdown = 0m;
        var withoutShowdown = 0m;
        var netWinningsWindow = 0m;
        var withShowdownWindow = 0m;
        var withoutShowdownWindow = 0m;

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
                var point = new WinrateHandBatchPointDto(
                    handCount,
                    net,
                    withShowdown,
                    withoutShowdown
                );
                points.Add(point);

                netWinningsWindow += point.NetWinningsBB;
                withShowdownWindow += point.WithShowdownWinningsBB;
                withoutShowdownWindow += point.WithoutShowdownWinningsBB;

                if (points.Count > MovingAverageWindowPoints)
                {
                    var expiredPoint = points[points.Count - MovingAverageWindowPoints - 1];
                    netWinningsWindow -= expiredPoint.NetWinningsBB;
                    withShowdownWindow -= expiredPoint.WithShowdownWinningsBB;
                    withoutShowdownWindow -= expiredPoint.WithoutShowdownWinningsBB;
                }

                var windowPointCount = Math.Min(points.Count, MovingAverageWindowPoints);
                points[^1] = point with
                {
                    NetWinningsBBMovingAverage = netWinningsWindow / windowPointCount,
                    WithShowdownWinningsBBMovingAverage = withShowdownWindow / windowPointCount,
                    WithoutShowdownWinningsBBMovingAverage =
                        withoutShowdownWindow / windowPointCount,
                };
            }
        }

        if (handCount % HandsPerPoint != 0)
        {
            var point = new WinrateHandBatchPointDto(handCount, net, withShowdown, withoutShowdown);
            points.Add(point);

            netWinningsWindow += point.NetWinningsBB;
            withShowdownWindow += point.WithShowdownWinningsBB;
            withoutShowdownWindow += point.WithoutShowdownWinningsBB;

            if (points.Count > MovingAverageWindowPoints)
            {
                var expiredPoint = points[points.Count - MovingAverageWindowPoints - 1];
                netWinningsWindow -= expiredPoint.NetWinningsBB;
                withShowdownWindow -= expiredPoint.WithShowdownWinningsBB;
                withoutShowdownWindow -= expiredPoint.WithoutShowdownWinningsBB;
            }

            var windowPointCount = Math.Min(points.Count, MovingAverageWindowPoints);
            points[^1] = point with
            {
                NetWinningsBBMovingAverage = netWinningsWindow / windowPointCount,
                WithShowdownWinningsBBMovingAverage = withShowdownWindow / windowPointCount,
                WithoutShowdownWinningsBBMovingAverage = withoutShowdownWindow / windowPointCount,
            };
        }

        return new WinrateGraphDto(points);
    }
}
