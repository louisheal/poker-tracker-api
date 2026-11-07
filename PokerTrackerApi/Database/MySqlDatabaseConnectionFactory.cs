using System.Data.Common;
using MySqlConnector;

namespace PokerTrackerApi.Database;

public sealed class MySqlDatabaseConnectionFactory : IPokerDatabaseConnectionFactory
{
    private readonly MySqlDataSource _dataSource;

    public MySqlDatabaseConnectionFactory(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken) =>
        await _dataSource.OpenConnectionAsync(cancellationToken);
}
