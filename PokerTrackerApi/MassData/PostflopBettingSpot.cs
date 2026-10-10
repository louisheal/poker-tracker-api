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

public enum PostflopBetResponseLine
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

    public PostflopResponseTo? ResponseTo { get; init; }

    public PostflopResponseAction? ResponseAction { get; init; }

    public PostflopBetResponseLine? BetResponseLine { get; init; }

    public decimal? BetToPotRatio { get; init; }
}
