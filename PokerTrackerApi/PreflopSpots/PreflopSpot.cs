using PokerTrackerApi.Domain;

namespace PokerTrackerApi.PreflopSpots;

public enum PreflopSpotState
{
    Unopened,
    Opened,
    ThreeBet,
}

public enum PreflopSpotScope
{
    Personal,
    Mda,
}

public class PreflopSpot
{
    public required string HandId { get; init; }

    public required string SpotKey { get; init; }

    public required PokerPosition ActorPosition { get; init; }

    public required PreflopSpotState StateBeforeAction { get; init; }

    public PokerPosition? OpenerPosition { get; init; }

    public PokerPosition? ThreeBettorPosition { get; init; }

    public required PokerAction Action { get; init; }

    public string? HandKey { get; init; }

    public bool IsHero { get; init; }
}
