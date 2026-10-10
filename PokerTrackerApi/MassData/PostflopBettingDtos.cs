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
