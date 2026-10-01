namespace PokerTrackerApi.PreflopSpots;

public interface IPreflopSpotRepository
{
    Task<RangeActionsDto> GetRangeAsync(string spotKey, CancellationToken cancellationToken);
}