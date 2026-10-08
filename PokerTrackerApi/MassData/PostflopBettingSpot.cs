using PokerTrackerApi.Domain;
using PokerTrackerApi.Domain.PokerHand;

namespace PokerTrackerApi.MassData;

public enum PostflopResponseAction
{
    Fold,
    Call,
    Raise,
}

public enum PostflopResponseTo
{
    PfrBet,
    DonkBet,
}

public enum RiverBetResponseLine
{
    BF,
    XBF,
}

public enum FlopTexture
{
    Monotone,
    TwoTone,
    Rainbow,
}

public enum PostflopActionSequence
{
    XX,
    XBC,
    XBRC,
    BC,
}

public enum FlopRankTexture
{
    Trips,
    Paired,
    Unpaired,
}

public enum PostflopRunout
{
    Overcard,
    FlushCompleting,
    Paired,
    Other,
}

public enum RiverShowdownOutcome
{
    HeroWin,
    VillainWin,
    Chop,
}

public enum VillainRiverShowdownOutcome
{
    Win,
    Loss,
    Chop,
}

public enum PostflopRiverBetSizeCategory
{
    Small,
    Medium,
    Large,
    Overbet,
}

public class PostflopBettingSpot
{
    public required string HandId { get; init; }

    public required string HeroPlayerId { get; init; }

    public DateTimeOffset HandTimestamp { get; init; }

    public PokerStreet Street { get; init; }

    public Rank? FlopHighCard { get; init; }

    public FlopTexture? FlopTexture { get; init; }

    public PostflopActionSequence? FlopActionSequence { get; init; }

    public FlopRankTexture? FlopRankTexture { get; init; }

    public PostflopActionSequence? TurnActionSequence { get; init; }

    public PostflopRunout? TurnRunout { get; init; }

    public PostflopRunout? RiverRunout { get; init; }

    public required string PfrPlayerId { get; init; }

    public required string DefendingPlayerId { get; init; }

    public PokerPosition PfrPosition { get; init; }

    public PokerPosition DefendingPosition { get; init; }

    public int PreflopRaiseCount { get; init; }

    public bool PfrInPosition { get; init; }

    public bool FlopWentCheckCheck { get; init; }

    public decimal? PfrBetBb { get; init; }

    public decimal? DonkBetBb { get; init; }

    public PostflopResponseTo? ResponseTo { get; init; }

    public PostflopResponseAction? ResponseAction { get; init; }

    public decimal? ResponseAmountBb { get; init; }

    public PostflopResponseAction? HeroResponseToVillainRiverBet { get; init; }

    public PostflopResponseAction? HeroResponseToVillainRiverRaise { get; init; }

    public PostflopResponseAction? VillainResponseToHeroRiverBet { get; init; }

    public decimal? HeroRiverBetToPotRatio { get; init; }

    public RiverBetResponseLine? RiverBetResponseLine { get; init; }

    public decimal? RiverBetToPotRatio { get; init; }

    public bool RiverWentToShowdown { get; init; }

    public bool VillainRiverBet { get; init; }

    public bool VillainRiverRaise { get; init; }

    public VillainRiverShowdownOutcome? VillainRiverBetShowdownOutcome { get; init; }

    public VillainRiverShowdownOutcome? VillainRiverRaiseShowdownOutcome { get; init; }

    public bool HeroCalledVillainRiverBet { get; init; }

    public bool HeroCalledVillainRiverRaise { get; init; }

    public RiverShowdownOutcome? RiverShowdownOutcome { get; init; }
}
