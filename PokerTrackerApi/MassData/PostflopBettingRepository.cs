using Microsoft.EntityFrameworkCore;
using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;
using PokerTrackerApi.Persistence;

namespace PokerTrackerApi.MassData;

public class PostflopBettingRepository : IPostflopBettingRepository
{
    private const string DelayedCBetContext =
        "Heads-up single-raised pot; flop checked through; PFR in position";

    private readonly PokerTrackerDbContext _dbContext;

    public PostflopBettingRepository(PokerTrackerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PostflopBettingResponseDto> GetPostflopBettingAsync(
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
        decimal? minRiverBetToPotPercent,
        decimal? maxRiverBetToPotPercent,
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
        var stats = new List<PostflopBettingStatDto>();

        stats.AddRange(
            await AggregateAsync(
                spots.Where(spot => spot.Street == PokerStreet.Flop && spot.PfrBetBb.HasValue),
                actorIsPfr: true,
                opportunityType: "FlopContinuationBet",
                responseTo: PostflopResponseTo.PfrBet,
                delayedContext: null,
                cancellationToken
            )
        );
        stats.AddRange(
            await AggregateAsync(
                spots.Where(spot => spot.Street == PokerStreet.Flop && spot.DonkBetBb.HasValue),
                actorIsPfr: false,
                opportunityType: "DonkBet",
                responseTo: PostflopResponseTo.DonkBet,
                delayedContext: null,
                cancellationToken
            )
        );
        stats.AddRange(
            await AggregateAsync(
                spots.Where(spot =>
                    spot.Street == PokerStreet.Turn
                    && spot.PreflopRaiseCount == 1
                    && spot.FlopWentCheckCheck
                    && spot.PfrInPosition
                    && spot.PfrBetBb.HasValue
                ),
                actorIsPfr: true,
                opportunityType: "DelayedContinuationBet",
                responseTo: PostflopResponseTo.PfrBet,
                delayedContext: DelayedCBetContext,
                cancellationToken
            )
        );

        var riverStats = await AggregateRiverAsync(spots, cancellationToken);
        var riverBetResponseStats = await AggregateRiverBetResponsesAsync(
            spots.Where(spot => spot.Street == PokerStreet.River),
            riverBetSizeCategory,
            minRiverBetToPotPercent,
            maxRiverBetToPotPercent,
            cancellationToken
        );

        return new PostflopBettingResponseDto(stats, riverStats, riverBetResponseStats);
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

    private static async Task<IReadOnlyList<PostflopBettingStatDto>> AggregateAsync(
        IQueryable<PostflopBettingSpot> spots,
        bool actorIsPfr,
        string opportunityType,
        PostflopResponseTo responseTo,
        string? delayedContext,
        CancellationToken cancellationToken
    )
    {
        var groupedStats = await spots
            .Select(spot => new
            {
                ActorPosition = actorIsPfr ? spot.PfrPosition : spot.DefendingPosition,
                ResponderPosition = actorIsPfr ? spot.DefendingPosition : spot.PfrPosition,
                ActorIsHero = actorIsPfr
                    ? spot.PfrPlayerId == spot.HeroPlayerId
                    : spot.DefendingPlayerId == spot.HeroPlayerId,
                ResponderIsHero = actorIsPfr
                    ? spot.DefendingPlayerId == spot.HeroPlayerId
                    : spot.PfrPlayerId == spot.HeroPlayerId,
                BetAmountBb = actorIsPfr ? spot.PfrBetBb : spot.DonkBetBb,
                ResponseAction = spot.ResponseTo == responseTo ? spot.ResponseAction : null,
            })
            .GroupBy(spot => new
            {
                spot.ActorPosition,
                spot.ResponderPosition,
                spot.ActorIsHero,
                spot.ResponderIsHero,
            })
            .Select(group => new
            {
                group.Key.ActorPosition,
                group.Key.ResponderPosition,
                group.Key.ActorIsHero,
                group.Key.ResponderIsHero,
                OpportunityCount = group.Count(),
                BetCount = group.Count(spot => spot.BetAmountBb > 0),
                FoldCount = group.Count(spot => spot.ResponseAction == PostflopResponseAction.Fold),
                CallCount = group.Count(spot => spot.ResponseAction == PostflopResponseAction.Call),
                RaiseCount = group.Count(spot =>
                    spot.ResponseAction == PostflopResponseAction.Raise
                ),
            })
            .ToArrayAsync(cancellationToken);

        return groupedStats
            .Select(group =>
            {
                var responseCount = group.FoldCount + group.CallCount + group.RaiseCount;
                var matchupDirection =
                    group.ActorIsHero ? "HeroVsVillain"
                    : group.ResponderIsHero ? "VillainVsHero"
                    : "VillainVsVillain";

                return new PostflopBettingStatDto(
                    opportunityType,
                    matchupDirection,
                    group.ActorPosition.ToString(),
                    group.ResponderPosition.ToString(),
                    delayedContext,
                    group.OpportunityCount,
                    group.BetCount,
                    (double)group.BetCount / group.OpportunityCount,
                    [
                        new("Fold", group.FoldCount, GetRate(group.FoldCount, responseCount)),
                        new("Call", group.CallCount, GetRate(group.CallCount, responseCount)),
                        new("Raise", group.RaiseCount, GetRate(group.RaiseCount, responseCount)),
                    ]
                );
            })
            .ToArray();
    }

    private static double GetRate(int count, int denominator) =>
        denominator == 0 ? 0 : (double)count / denominator;

    private static async Task<IReadOnlyList<PostflopRiverBettingStatDto>> AggregateRiverAsync(
        IQueryable<PostflopBettingSpot> spots,
        CancellationToken cancellationToken
    )
    {
        var riverSpots = spots.Where(spot => spot.Street == PokerStreet.River);
        var stats = new List<PostflopRiverBettingStatDto>();
        stats.AddRange(
            await AggregateRiverActionAsync(
                riverSpots.Where(spot => spot.VillainRiverBet),
                "Bet",
                isRaise: false,
                cancellationToken
            )
        );
        stats.AddRange(
            await AggregateRiverActionAsync(
                riverSpots.Where(spot => spot.VillainRiverRaise),
                "Raise",
                isRaise: true,
                cancellationToken
            )
        );
        return stats;
    }

    private static async Task<IReadOnlyList<PostflopRiverBettingStatDto>> AggregateRiverActionAsync(
        IQueryable<PostflopBettingSpot> spots,
        string aggressionType,
        bool isRaise,
        CancellationToken cancellationToken
    )
    {
        var groupedStats = await spots
            .Select(spot => new
            {
                HeroIsInHand = spot.HeroPlayerId == spot.PfrPlayerId
                    || spot.HeroPlayerId == spot.DefendingPlayerId,
                VillainPosition = spot.HeroPlayerId == spot.PfrPlayerId
                    ? (PokerPosition?)spot.DefendingPosition
                : spot.HeroPlayerId == spot.DefendingPlayerId ? spot.PfrPosition
                : null,
                HeroPosition = spot.HeroPlayerId == spot.PfrPlayerId
                    ? (PokerPosition?)spot.PfrPosition
                : spot.HeroPlayerId == spot.DefendingPlayerId ? spot.DefendingPosition
                : null,
                HeroCalled = isRaise
                    ? spot.HeroCalledVillainRiverRaise
                    : spot.HeroCalledVillainRiverBet,
                VillainShowdownOutcome = isRaise
                    ? spot.VillainRiverRaiseShowdownOutcome
                    : spot.VillainRiverBetShowdownOutcome,
                spot.RiverWentToShowdown,
                spot.RiverShowdownOutcome,
            })
            .GroupBy(spot => new
            {
                spot.VillainPosition,
                spot.HeroPosition,
                spot.HeroIsInHand,
            })
            .Select(group => new
            {
                group.Key.VillainPosition,
                group.Key.HeroPosition,
                group.Key.HeroIsInHand,
                OpportunityCount = group.Count(),
                HeroOpportunityCount = group.Count(spot => spot.HeroIsInHand),
                ShowdownCount = group.Count(spot => spot.RiverWentToShowdown),
                VillainWinCount = group.Count(spot =>
                    spot.VillainShowdownOutcome == VillainRiverShowdownOutcome.Win
                ),
                OpponentWinCount = group.Count(spot =>
                    spot.VillainShowdownOutcome == VillainRiverShowdownOutcome.Loss
                ),
                ChopCount = group.Count(spot =>
                    spot.VillainShowdownOutcome == VillainRiverShowdownOutcome.Chop
                ),
                HeroCallCount = group.Count(spot => spot.HeroCalled),
                HeroCallVillainWinCount = group.Count(spot =>
                    spot.HeroCalled && spot.RiverShowdownOutcome == RiverShowdownOutcome.VillainWin
                ),
                HeroCallHeroWinCount = group.Count(spot =>
                    spot.HeroCalled && spot.RiverShowdownOutcome == RiverShowdownOutcome.HeroWin
                ),
                HeroCallChopCount = group.Count(spot =>
                    spot.HeroCalled && spot.RiverShowdownOutcome == RiverShowdownOutcome.Chop
                ),
            })
            .ToArrayAsync(cancellationToken);

        return groupedStats
            .Select(group => new PostflopRiverBettingStatDto(
                aggressionType,
                group.VillainPosition?.ToString() ?? "VillainVsVillain",
                group.HeroPosition?.ToString() ?? "NotInHand",
                group.OpportunityCount,
                group.HeroOpportunityCount,
                group.ShowdownCount,
                group.VillainWinCount,
                group.OpponentWinCount,
                group.ChopCount,
                group.HeroCallCount,
                group.HeroCallVillainWinCount,
                group.HeroCallHeroWinCount,
                group.HeroCallChopCount
            ))
            .ToArray();
    }

    private static async Task<
        IReadOnlyList<PostflopRiverBetResponseStatDto>
    > AggregateRiverBetResponsesAsync(
        IQueryable<PostflopBettingSpot> spots,
        PostflopRiverBetSizeCategory? sizeCategory,
        decimal? minBetToPotPercent,
        decimal? maxBetToPotPercent,
        CancellationToken cancellationToken
    )
    {
        var minRatio = minBetToPotPercent / 100m;
        var maxRatio = maxBetToPotPercent / 100m;
        var riverBetResponses = spots.Where(spot =>
            spot.ResponseTo.HasValue
            && spot.ResponseAction.HasValue
            && spot.RiverBetResponseLine.HasValue
        );
        if (sizeCategory.HasValue || minBetToPotPercent.HasValue || maxBetToPotPercent.HasValue)
        {
            riverBetResponses = riverBetResponses.Where(spot => spot.RiverBetToPotRatio.HasValue);
        }

        if (sizeCategory.HasValue)
        {
            riverBetResponses = sizeCategory.Value switch
            {
                PostflopRiverBetSizeCategory.Small => riverBetResponses.Where(spot =>
                    spot.RiverBetToPotRatio < 0.4m
                ),
                PostflopRiverBetSizeCategory.Medium => riverBetResponses.Where(spot =>
                    spot.RiverBetToPotRatio >= 0.4m && spot.RiverBetToPotRatio < 0.7m
                ),
                PostflopRiverBetSizeCategory.Large => riverBetResponses.Where(spot =>
                    spot.RiverBetToPotRatio >= 0.7m && spot.RiverBetToPotRatio <= 1m
                ),
                PostflopRiverBetSizeCategory.Overbet => riverBetResponses.Where(spot =>
                    spot.RiverBetToPotRatio > 1m
                ),
                _ => riverBetResponses,
            };
        }

        if (minRatio.HasValue)
        {
            riverBetResponses = riverBetResponses.Where(spot =>
                spot.RiverBetToPotRatio >= minRatio.Value
            );
        }

        if (maxRatio.HasValue)
        {
            riverBetResponses = riverBetResponses.Where(spot =>
                spot.RiverBetToPotRatio <= maxRatio.Value
            );
        }

        var responses = await riverBetResponses
            .Select(spot => new
            {
                ResponderIsHero = spot.ResponseTo == PostflopResponseTo.PfrBet
                    ? spot.HeroPlayerId == spot.DefendingPlayerId
                    : spot.HeroPlayerId == spot.PfrPlayerId,
                RiverBetResponseLine = spot.RiverBetResponseLine!.Value,
                spot.ResponseAction,
            })
            .ToArrayAsync(cancellationToken);

        var villainResponses = responses.Where(response => !response.ResponderIsHero).ToArray();
        return Enum.GetValues<RiverBetResponseLine>()
            .Select(line =>
            {
                var lineResponses = villainResponses
                    .Where(response => response.RiverBetResponseLine == line)
                    .ToArray();
                return new PostflopRiverBetResponseStatDto(
                    line.ToString(),
                    lineResponses.Length,
                    lineResponses.Count(response =>
                        response.ResponseAction == PostflopResponseAction.Fold
                    )
                );
            })
            .ToArray();
    }
}
