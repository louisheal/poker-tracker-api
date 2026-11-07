using System.Data.Common;

namespace PokerTrackerApi.Database;

public interface IPokerDatabaseConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
