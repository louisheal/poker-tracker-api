using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Metrics;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.Diagnostics;

public interface IRiverDiagnosticsRepository
{
    Task<RiverDiagnosticsDto> GetRiverDiagnosticsAsync(CancellationToken cancellationToken);
}

public class RiverDiagnosticsRepository : IRiverDiagnosticsRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public RiverDiagnosticsRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RiverDiagnosticsDto> GetRiverDiagnosticsAsync(
        CancellationToken cancellationToken
    )
    {
        var hands = _dbContext
            .PokerHands.AsNoTracking()
            .Include(hand => hand.Events)
            .AsSingleQuery()
            .AsAsyncEnumerable();

        var totals = Enum.GetValues<RiverSpotType>()
            .ToDictionary(spot => spot, _ => new SpotTotals());

        await foreach (var hand in hands.WithCancellation(cancellationToken))
        {
            var result = HeroHandResultCalculator.Calculate(hand);
            if (!result.WentToShowdown)
            {
                continue;
            }

            var classification = RiverSpotClassifier.Classify(hand);
            if (classification is null)
            {
                continue;
            }

            var spotTotals = totals[classification.Spot];
            spotTotals.Add(hand.HandId, result.NetWinningsBB);
            if (classification.SizeCategory is not null)
            {
                spotTotals
                    .SizeTotals[classification.SizeCategory.Value]
                    .Add(hand.HandId, result.NetWinningsBB);
            }
        }

        var rows = totals
            .OrderBy(entry => entry.Key)
            .Select(entry =>
            {
                var sizeBreakdown =
                    entry.Key == RiverSpotType.HeroCheckRiver
                        ? []
                        : entry
                            .Value.SizeTotals.OrderBy(sizeEntry => sizeEntry.Key)
                            .Select(sizeEntry => new RiverBetSizeRowDto(
                                sizeEntry.Key.ToString(),
                                sizeEntry.Value.Hands,
                                sizeEntry.Value.GetWinningsBBPer100(),
                                sizeEntry.Value.HandIds
                            ))
                            .ToList();

                return new RiverSpotRowDto(
                    entry.Key.ToString(),
                    entry.Value.Hands,
                    entry.Value.GetWinningsBBPer100(),
                    sizeBreakdown,
                    entry.Value.HandIds
                );
            })
            .ToList();

        return new RiverDiagnosticsDto(rows);
    }

    private class SpotTotals
    {
        public int Hands { get; private set; }
        private decimal NetBB { get; set; }
        public List<string> HandIds { get; } = [];
        public Dictionary<RiverBetSizeCategory, SizeTotals> SizeTotals { get; } =
            Enum.GetValues<RiverBetSizeCategory>()
                .ToDictionary(category => category, _ => new SizeTotals());

        public void Add(string handId, decimal netBB)
        {
            Hands++;
            NetBB += netBB;
            HandIds.Add(handId);
        }

        public decimal GetWinningsBBPer100() => Hands == 0 ? 0m : NetBB / Hands * 100m;
    }

    private class SizeTotals
    {
        public int Hands { get; private set; }
        private decimal NetBB { get; set; }
        public List<string> HandIds { get; } = [];

        public void Add(string handId, decimal netBB)
        {
            Hands++;
            NetBB += netBB;
            HandIds.Add(handId);
        }

        public decimal GetWinningsBBPer100() => Hands == 0 ? 0m : NetBB / Hands * 100m;
    }
}
