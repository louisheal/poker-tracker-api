using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.MassData;

public class PostflopBettingRepository : IPostflopBettingRepository
{
    private static readonly string[] FoldEquityLines = ["BF", "XBF", "BRF", "XBRF"];

    private static readonly string[] FoldEquityDecisionContexts = ["B", "XB", "BR", "XBR"];

    private static readonly IReadOnlyDictionary<string, string> FoldEquityLinesByContext = new Dictionary<
        string,
        string
    >
    {
        ["B"] = "BF",
        ["XB"] = "XBF",
        ["BR"] = "BRF",
        ["XBR"] = "XBRF",
    };

    private static readonly int[] ResponseBucketThresholds = Enumerable
        .Range(1, 30)
        .Select(value => value * 10)
        .ToArray();

    private readonly PokerTrackerDbContext _dbContext;

    public PostflopBettingRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PostflopBetResponseBucketsDto> GetResponseBucketsAsync(
        PokerStreet street,
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes,
        IReadOnlyCollection<PostflopActionSequence>? flopActionSequences,
        IReadOnlyCollection<FlopRankTexture>? flopRankTextures,
        IReadOnlyCollection<PostflopActionSequence>? turnActionSequences,
        IReadOnlyCollection<PostflopRunout>? turnRunouts,
        IReadOnlyCollection<PostflopRunout>? riverRunouts,
        PostflopRiverBetSizeCategory? riverBetSizeCategory,
        CancellationToken cancellationToken
    )
    {
        var spots = ApplyFilters(
            _dbContext.PostflopBettingSpots.AsNoTracking(),
            pfrInPosition,
            ipPosition,
            oopPosition,
            flopHighCard,
            flopTextures,
            potTypes,
            flopActionSequences,
            flopRankTextures,
            turnActionSequences,
            turnRunouts,
            riverRunouts
        );

        if (street == PokerStreet.Preflop)
        {
            throw new ArgumentOutOfRangeException(nameof(street), street, null);
        }

        var streetSpots = spots.Where(spot => spot.Street == street);
        return await AggregateBetResponseBucketsAsync(
            streetSpots,
            street == PokerStreet.River ? riverBetSizeCategory : null,
            cancellationToken
        );
    }

    public async Task<PostflopActionFrequenciesDto> GetActionFrequenciesAsync(
        PokerStreet street,
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes,
        IReadOnlyCollection<PostflopActionSequence>? flopActionSequences,
        IReadOnlyCollection<FlopRankTexture>? flopRankTextures,
        IReadOnlyCollection<PostflopActionSequence>? turnActionSequences,
        IReadOnlyCollection<PostflopRunout>? turnRunouts,
        IReadOnlyCollection<PostflopRunout>? riverRunouts,
        string? decisionContext,
        CancellationToken cancellationToken
    )
    {
        if (street == PokerStreet.Preflop)
        {
            throw new ArgumentOutOfRangeException(nameof(street), street, null);
        }

        var spots = ApplyFilters(
            _dbContext.PostflopBettingSpots.AsNoTracking(),
            pfrInPosition,
            ipPosition,
            oopPosition,
            flopHighCard,
            flopTextures,
            potTypes,
            flopActionSequences,
            flopRankTextures,
            turnActionSequences,
            turnRunouts,
            riverRunouts
        );

        var actions = await spots
            .Where(spot =>
                spot.Street == street
                && spot.ActorPlayerId != string.Empty
                && (decisionContext == null || spot.DecisionContext == decisionContext)
            )
            .Select(spot => new
            {
                spot.DecisionContext,
                spot.ActorPlayerId,
                spot.HeroPlayerId,
                spot.PfrPlayerId,
                spot.Action,
            })
            .ToArrayAsync(cancellationToken);

        var populationActions = actions
            .Where(action =>
                !string.Equals(action.ActorPlayerId, action.HeroPlayerId, StringComparison.Ordinal)
            )
            .Select(action => new
            {
                action.DecisionContext,
                ActorIsPfr = string.Equals(
                    action.ActorPlayerId,
                    action.PfrPlayerId,
                    StringComparison.Ordinal
                ),
                action.Action,
            });

        var frequencies = populationActions
            .GroupBy(action => new { action.DecisionContext, action.ActorIsPfr })
            .OrderBy(group => group.Key.DecisionContext)
            .ThenBy(group => group.Key.ActorIsPfr)
            .SelectMany(group =>
                Enum.GetValues<PostflopActionType>()
                    .Select(actionType =>
                    {
                        var actionCount = group.Count(action => action.Action == actionType);
                        return new PostflopActionFrequencyDto(
                            GetActionPath(group.Key.DecisionContext, actionType),
                            group.Key.DecisionContext,
                            group.Key.ActorIsPfr ? "Pfr" : "Defender",
                            actionType.ToString(),
                            actionCount,
                            group.Count()
                        );
                    })
            )
            .ToArray();

        return new PostflopActionFrequenciesDto(frequencies);
    }

    private static IQueryable<PostflopBettingSpot> ApplyFilters(
        IQueryable<PostflopBettingSpot> spots,
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes,
        IReadOnlyCollection<PostflopActionSequence>? flopActionSequences,
        IReadOnlyCollection<FlopRankTexture>? flopRankTextures,
        IReadOnlyCollection<PostflopActionSequence>? turnActionSequences,
        IReadOnlyCollection<PostflopRunout>? turnRunouts,
        IReadOnlyCollection<PostflopRunout>? riverRunouts
    )
    {
        if (pfrInPosition.HasValue)
        {
            spots = spots.Where(spot => spot.PfrInPosition == pfrInPosition.Value);
        }

        if (ipPosition.HasValue)
        {
            spots = spots.Where(spot =>
                spot.PfrInPosition
                    ? spot.PfrPosition == ipPosition.Value
                    : spot.DefendingPosition == ipPosition.Value
            );
        }

        if (oopPosition.HasValue)
        {
            spots = spots.Where(spot =>
                spot.PfrInPosition
                    ? spot.DefendingPosition == oopPosition.Value
                    : spot.PfrPosition == oopPosition.Value
            );
        }

        if (flopHighCard.HasValue)
        {
            spots = spots.Where(spot => spot.FlopHighCard == flopHighCard.Value);
        }

        if (flopTextures is { Count: > 0 })
        {
            var selectedTextures = flopTextures.ToList();
            spots = spots.Where(spot =>
                spot.FlopTexture.HasValue && selectedTextures.Contains(spot.FlopTexture.Value)
            );
        }

        if (potTypes is { Count: > 0 })
        {
            var raiseCounts = potTypes.Select(GetPreflopRaiseCount).ToList();
            spots = spots.Where(spot => raiseCounts.Contains(spot.PreflopRaiseCount));
        }

        if (flopActionSequences is { Count: > 0 })
        {
            var selectedSequences = flopActionSequences.ToList();
            spots = spots.Where(spot =>
                spot.FlopActionSequence.HasValue
                && selectedSequences.Contains(spot.FlopActionSequence.Value)
            );
        }

        if (flopRankTextures is { Count: > 0 })
        {
            var selectedTextures = flopRankTextures.ToList();
            spots = spots.Where(spot =>
                spot.FlopRankTexture.HasValue
                && selectedTextures.Contains(spot.FlopRankTexture.Value)
            );
        }

        if (turnActionSequences is { Count: > 0 })
        {
            var selectedSequences = turnActionSequences.ToList();
            spots = spots.Where(spot =>
                spot.TurnActionSequence.HasValue
                && selectedSequences.Contains(spot.TurnActionSequence.Value)
            );
        }

        if (turnRunouts is { Count: > 0 })
        {
            var selectedRunouts = turnRunouts.ToList();
            spots = spots.Where(spot =>
                spot.TurnRunout.HasValue && selectedRunouts.Contains(spot.TurnRunout.Value)
            );
        }

        if (riverRunouts is { Count: > 0 })
        {
            var selectedRunouts = riverRunouts.ToList();
            spots = spots.Where(spot =>
                spot.RiverRunout.HasValue && selectedRunouts.Contains(spot.RiverRunout.Value)
            );
        }

        return spots;
    }

    private static int GetPreflopRaiseCount(PostflopPotType potType) =>
        potType switch
        {
            PostflopPotType.SingleRaisedPot => 1,
            PostflopPotType.ThreeBetPot => 2,
            PostflopPotType.FourBetPot => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(potType), potType, null),
        };

    private static string GetActionPath(string decisionContext, PostflopActionType actionType) =>
        decisionContext
        + (actionType switch
        {
            PostflopActionType.Check => "X",
            PostflopActionType.Bet => "B",
            PostflopActionType.Raise => "R",
            PostflopActionType.Call => "C",
            PostflopActionType.Fold => "F",
            _ => throw new ArgumentOutOfRangeException(nameof(actionType), actionType, null),
        });

    private static async Task<PostflopBetResponseBucketsDto> AggregateBetResponseBucketsAsync(
        IQueryable<PostflopBettingSpot> spots,
        PostflopRiverBetSizeCategory? riverBetSizeCategory,
        CancellationToken cancellationToken
    )
    {
        var decisionContexts = FoldEquityDecisionContexts;
        var aggressionActionPaths = FoldEquityDecisionContexts;
        var relevantSpots = await spots
            .Where(spot =>
                spot.ActorPlayerId != string.Empty
                && (
                    Enumerable.Contains(decisionContexts, spot.DecisionContext)
                    || Enumerable.Contains(aggressionActionPaths, spot.ActionPath)
                )
            )
            .Select(spot => new
            {
                spot.HandId,
                spot.ActionPath,
                spot.DecisionContext,
                spot.ActorPlayerId,
                spot.HeroPlayerId,
                spot.Action,
                spot.ActionSizeToPotRatio,
            })
            .ToArrayAsync(cancellationToken);

        var aggressiveActionSizes = relevantSpots
            .Where(spot => aggressionActionPaths.Contains(spot.ActionPath))
            .ToDictionary(
                spot => (spot.HandId, spot.ActionPath),
                spot => spot.ActionSizeToPotRatio
            );

        var populationResponses = relevantSpots
            .Where(spot =>
                decisionContexts.Contains(spot.DecisionContext)
                && !string.Equals(spot.ActorPlayerId, spot.HeroPlayerId, StringComparison.Ordinal)
                && spot.Action.HasValue
                && aggressiveActionSizes.ContainsKey((spot.HandId, spot.DecisionContext))
                && MatchesRiverBetSizeCategory(
                    aggressiveActionSizes[(spot.HandId, spot.DecisionContext)],
                    riverBetSizeCategory
                )
            )
            .Select(spot =>
                new PostflopBetResponseObservation(
                    FoldEquityLinesByContext[spot.DecisionContext],
                    spot.Action!.Value,
                    aggressiveActionSizes[(spot.HandId, spot.DecisionContext)]
                )
            );

        return new(AggregateResponseBuckets(populationResponses));
    }

    private static IReadOnlyList<PostflopBetResponseBucketDto> AggregateResponseBuckets(
        IEnumerable<PostflopBetResponseObservation> observations
    )
    {
        var countsByLine = FoldEquityLines
            .ToDictionary(
                line => line,
                _ =>
                    (
                        Opportunities: new int[ResponseBucketThresholds.Length],
                        Folds: new int[ResponseBucketThresholds.Length]
                    )
            );

        foreach (var observation in observations)
        {
            if (!observation.BetToPotRatio.HasValue || observation.BetToPotRatio.Value > 3m)
            {
                continue;
            }

            var counts = countsByLine[observation.Line];
            for (
                var thresholdIndex = 0;
                thresholdIndex < ResponseBucketThresholds.Length;
                thresholdIndex++
            )
            {
                if (
                    observation.BetToPotRatio.Value
                    > ResponseBucketThresholds[thresholdIndex] / 100m
                )
                {
                    continue;
                }

                counts.Opportunities[thresholdIndex]++;
                if (observation.Action == PostflopActionType.Fold)
                {
                    counts.Folds[thresholdIndex]++;
                }
            }
        }

        return FoldEquityLines
            .SelectMany(line =>
                ResponseBucketThresholds.Select(
                    (threshold, index) =>
                    {
                        var counts = countsByLine[line];
                        return new PostflopBetResponseBucketDto(
                            line.ToString(),
                            threshold,
                            counts.Opportunities[index],
                            counts.Folds[index]
                        );
                    }
                )
            )
            .ToArray();
    }

    private record PostflopBetResponseObservation(
        string Line,
        PostflopActionType Action,
        decimal? BetToPotRatio
    );

    private static bool MatchesRiverBetSizeCategory(
        decimal? betToPotRatio,
        PostflopRiverBetSizeCategory? category
    ) =>
        !category.HasValue
        || (category.Value switch
        {
            PostflopRiverBetSizeCategory.Small => betToPotRatio < 0.4m,
            PostflopRiverBetSizeCategory.Medium => betToPotRatio >= 0.4m && betToPotRatio < 0.7m,
            PostflopRiverBetSizeCategory.Large => betToPotRatio >= 0.7m && betToPotRatio <= 1m,
            PostflopRiverBetSizeCategory.Overbet => betToPotRatio > 1m,
            _ => true,
        });
}
