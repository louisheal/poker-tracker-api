using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Metrics;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.Diagnostics;

public interface ITurnDiagnosticsRepository
{
    Task<TurnDiagnosticsDto> GetTurnDiagnosticsAsync(
        IReadOnlyCollection<TurnPotType>? potTypes,
        CancellationToken cancellationToken
    );
}

public class TurnDiagnosticsRepository : ITurnDiagnosticsRepository
{
    private readonly PokerTrackerDbContext _dbContext;

    public TurnDiagnosticsRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TurnDiagnosticsDto> GetTurnDiagnosticsAsync(
        IReadOnlyCollection<TurnPotType>? potTypes,
        CancellationToken cancellationToken
    )
    {
        var selectedPotTypes =
            potTypes is { Count: > 0 } ? potTypes.ToHashSet() : Enum.GetValues<TurnPotType>().ToHashSet();
        var totals = Enum.GetValues<TurnSpotType>()
            .ToDictionary(
                spot => spot,
                _ => Enum.GetValues<TurnActionType>().ToDictionary(action => action, _ => new SpotTotals())
            );

        var hands = _dbContext
            .PokerHands.AsNoTracking()
            .Include(hand => hand.Events)
            .Include(hand => hand.Players)
            .AsSplitQuery()
            .AsAsyncEnumerable();

        await foreach (var hand in hands.WithCancellation(cancellationToken))
        {
            var classification = TurnSpotClassifier.Classify(hand);
            if (classification is null || !selectedPotTypes.Contains(classification.PotType))
            {
                continue;
            }

            var result = HeroHandResultCalculator.Calculate(hand);
            totals[classification.Spot][classification.Action].Add(
                hand.HandId,
                result.NetWinningsBB
            );
        }

        var rows = totals
            .OrderBy(entry => entry.Key)
            .Select(entry =>
            {
                var actionRows = entry
                    .Value.OrderBy(actionEntry => actionEntry.Key)
                    .Select(actionEntry =>
                        new TurnActionRowDto(
                            actionEntry.Key.ToString(),
                            actionEntry.Value.Hands,
                            actionEntry.Value.GetWinningsBBPer100(),
                            actionEntry.Value.HandIds
                        )
                    )
                    .ToList();
                var groupTotals = actionRows.SelectMany(row => row.HandIds).ToList();
                var groupNetBB = entry.Value.Values.Sum(actionTotals => actionTotals.NetBB);
                var groupHands = groupTotals.Count;

                return new TurnSpotRowDto(
                    entry.Key.ToString(),
                    groupHands,
                    groupHands == 0 ? 0m : groupNetBB / groupHands * 100m,
                    groupTotals,
                    actionRows
                );
            })
            .ToList();

        return new TurnDiagnosticsDto(rows);
    }

    private class SpotTotals
    {
        public int Hands { get; private set; }
        public decimal NetBB { get; private set; }
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
