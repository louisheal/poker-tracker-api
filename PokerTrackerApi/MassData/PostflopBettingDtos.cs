namespace PokerTrackerApi.MassData;

public enum PostflopPotType
{
    SingleRaisedPot,
    ThreeBetPot,
    FourBetPot,
}

public record PostflopBettingResponseDto(
    IReadOnlyList<PostflopBettingStatDto> Stats,
    IReadOnlyList<PostflopRiverBettingStatDto> RiverStats,
    IReadOnlyList<PostflopRiverBetResponseStatDto> RiverBetResponseStats
);

public record PostflopRiverBetResponseStatDto(
    string Line,
    int OpportunityCount,
    int VillainFoldCount
);

public record PostflopRiverBettingStatDto(
    string AggressionType,
    string VillainPosition,
    string HeroPosition,
    int OpportunityCount,
    int HeroOpportunityCount,
    int ShowdownCount,
    int VillainWinCount,
    int OpponentWinCount,
    int ChopCount,
    int HeroCallCount,
    int HeroCallVillainWinCount,
    int HeroCallHeroWinCount,
    int HeroCallChopCount,
    int VillainFoldCount = 0
);

public record PostflopBettingStatDto(
    string OpportunityType,
    string MatchupDirection,
    string ActorPosition,
    string? ResponderPosition,
    string? DelayedContext,
    int OpportunityCount,
    int BetCount,
    double BetRate,
    IReadOnlyList<PostflopBettingResponseBreakdownDto> ResponseBreakdown
);

public record PostflopBettingResponseBreakdownDto(string ResponseType, int Count, double Rate);
