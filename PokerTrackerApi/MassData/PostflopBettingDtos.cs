namespace PokerTrackerApi.MassData;

public enum PostflopPotType
{
    SingleRaisedPot,
    ThreeBetPot,
    FourBetPot,
}

public record PostflopBettingResponseDto(IReadOnlyList<PostflopBettingStatDto> Stats);

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
