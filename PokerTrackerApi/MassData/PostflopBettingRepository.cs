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
            potTypes
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

        return new PostflopBettingResponseDto(stats);
    }

    private static IQueryable<PostflopBettingSpot> ApplyFilters(
        IQueryable<PostflopBettingSpot> spots,
        bool? pfrInPosition,
        PokerPosition? ipPosition,
        PokerPosition? oopPosition,
        Rank? flopHighCard,
        IReadOnlyCollection<FlopTexture>? flopTextures,
        IReadOnlyCollection<PostflopPotType>? potTypes
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
}
