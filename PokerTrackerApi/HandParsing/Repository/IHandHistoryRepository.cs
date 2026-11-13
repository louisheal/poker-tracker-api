using PokerTrackerApi.Domain.HandHistories;

namespace PokerTrackerApi.HandParsing.Repository;

public interface IHandHistoryRepository
{
    Task SaveHandHistory(HandHistory handHistory, CancellationToken cancellationToken);
}
