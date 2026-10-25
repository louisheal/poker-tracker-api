using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.MassData;

public class PostflopBettingRepository : IPostflopBettingRepository
{
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
        if (street == PokerStreet.River && riverBetSizeCategory.HasValue)
        {
            streetSpots = riverBetSizeCategory.Value switch
            {
                PostflopRiverBetSizeCategory.Small => streetSpots.Where(spot =>
                    spot.BetToPotRatio < 0.4m
                ),
                PostflopRiverBetSizeCategory.Medium => streetSpots.Where(spot =>
                    spot.BetToPotRatio >= 0.4m && spot.BetToPotRatio < 0.7m
                ),
                PostflopRiverBetSizeCategory.Large => streetSpots.Where(spot =>
                    spot.BetToPotRatio >= 0.7m && spot.BetToPotRatio <= 1m
                ),
                PostflopRiverBetSizeCategory.Overbet => streetSpots.Where(spot =>
                    spot.BetToPotRatio > 1m
                ),
                _ => streetSpots,
            };
        }

        return await AggregateBetResponseBucketsAsync(streetSpots, street, cancellationToken);
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

    private static async Task<PostflopBetResponseBucketsDto> AggregateBetResponseBucketsAsync(
        IQueryable<PostflopBettingSpot> spots,
        PokerStreet street,
        CancellationToken cancellationToken
    )
    {
        var responses = await spots
            .Where(spot =>
                spot.Street == street
                && spot.ResponseTo.HasValue
                && spot.ResponseAction.HasValue
                && spot.BetResponseLine.HasValue
            )
            .Select(spot => new
            {
                ResponderIsHero = spot.ResponseTo == PostflopResponseTo.PfrBet
                    ? spot.HeroPlayerId == spot.DefendingPlayerId
                    : spot.HeroPlayerId == spot.PfrPlayerId,
                Line = spot.BetResponseLine!.Value,
                Action = spot.ResponseAction!.Value,
                spot.BetToPotRatio,
            })
            .ToArrayAsync(cancellationToken);

        var populationResponses = responses
            .Where(response => !response.ResponderIsHero)
            .Select(response => new PostflopBetResponseObservation(
                response.Line,
                response.Action,
                response.BetToPotRatio
            ));

        return new(AggregateResponseBuckets(populationResponses));
    }

    private static IReadOnlyList<PostflopBetResponseBucketDto> AggregateResponseBuckets(
        IEnumerable<PostflopBetResponseObservation> observations
    )
    {
        var countsByLine = Enum.GetValues<PostflopBetResponseLine>()
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
                if (observation.Action == PostflopResponseAction.Fold)
                {
                    counts.Folds[thresholdIndex]++;
                }
            }
        }

        return Enum.GetValues<PostflopBetResponseLine>()
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
        PostflopBetResponseLine Line,
        PostflopResponseAction Action,
        decimal? BetToPotRatio
    );
}
