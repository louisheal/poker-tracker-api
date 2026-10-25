namespace PokerTrackerApi.MassData;

public enum PostflopPotType
{
    SingleRaisedPot,
    ThreeBetPot,
    FourBetPot,
}

public record PostflopBetResponseBucketDto(
    string Line,
    int BetSizeThresholdPercent,
    int OpportunityCount,
    int VillainFoldCount
);

public record PostflopBetResponseBucketsDto(IReadOnlyList<PostflopBetResponseBucketDto> Buckets);

public record PostflopActionFrequencyDto(
    string Line,
    string DecisionContext,
    string ActorRole,
    string Action,
    int ActionCount,
    int OpportunityCount
);

public record PostflopActionFrequenciesDto(IReadOnlyList<PostflopActionFrequencyDto> Frequencies);
