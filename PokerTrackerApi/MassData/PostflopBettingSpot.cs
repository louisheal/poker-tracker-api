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

public enum FlopTexture
{
    Monotone,
    TwoTone,
    Rainbow,
}

public class PostflopBettingSpot
{
    public required string HandId { get; init; }

    public required string HeroPlayerId { get; init; }

    public DateTimeOffset HandTimestamp { get; init; }

    public PokerStreet Street { get; init; }

    public Rank? FlopHighCard { get; init; }

    public FlopTexture? FlopTexture { get; init; }

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
}
