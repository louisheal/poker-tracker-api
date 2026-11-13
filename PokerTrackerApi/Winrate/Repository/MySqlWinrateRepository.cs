using System.Runtime.CompilerServices;
using Dapper;
using PokerTrackerApi.Database;

namespace PokerTrackerApi.Winrate.Repository;

public class MySqlWinrateRepository : IWinrateRepository
{
    private readonly IPokerDatabaseConnectionFactory _connectionFactory;

    public MySqlWinrateRepository(IPokerDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async IAsyncEnumerable<Winrate> GetWinrates(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        const string query = """
                SELECT
                    NetWonBB,
                    WonWithShowdownBB,
                    WonWithoutShowdownBB,
                    NetWonBeforeRakeBB,
                    WonWithShowdownBeforeRakeBB,
                    WonWithoutShowdownBeforeRakeBB
                FROM Winrate
                ORDER BY HandId;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        await foreach (
            var dao in connection
                .QueryUnbufferedAsync<WinrateDao>(query)
                .WithCancellation(cancellationToken)
        )
        {
            yield return new Winrate(
                dao.NetWonBB,
                dao.WonWithShowdownBB,
                dao.WonWithoutShowdownBB,
                dao.NetWonBeforeRakeBB,
                dao.WonWithShowdownBeforeRakeBB,
                dao.WonWithoutShowdownBeforeRakeBB
            );
        }
    }

    public async Task SaveWinrate(
        Winrate winrate,
        string handId,
        CancellationToken cancellationToken
    )
    {
        const string query = """
                INSERT INTO Winrate
                    (
                        HandId,
                        NetWonBB,
                        WonWithShowdownBB,
                        WonWithoutShowdownBB,
                        NetWonBeforeRakeBB,
                        WonWithShowdownBeforeRakeBB,
                        WonWithoutShowdownBeforeRakeBB
                    )
                VALUES
                    (
                        @HandId,
                        @NetWonBB,
                        @WonWithShowdownBB,
                        @WonWithoutShowdownBB,
                        @NetWonBeforeRakeBB,
                        @WonWithShowdownBeforeRakeBB,
                        @WonWithoutShowdownBeforeRakeBB
                    )
                ON DUPLICATE KEY UPDATE
                    NetWonBB = VALUES(NetWonBB),
                    WonWithShowdownBB = VALUES(WonWithShowdownBB),
                    WonWithoutShowdownBB = VALUES(WonWithoutShowdownBB),
                    NetWonBeforeRakeBB = VALUES(NetWonBeforeRakeBB),
                    WonWithShowdownBeforeRakeBB = VALUES(WonWithShowdownBeforeRakeBB),
                    WonWithoutShowdownBeforeRakeBB = VALUES(WonWithoutShowdownBeforeRakeBB);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                query,
                new
                {
                    HandId = handId,
                    winrate.NetWonBB,
                    winrate.WonWithShowdownBB,
                    winrate.WonWithoutShowdownBB,
                    winrate.NetWonBeforeRakeBB,
                    winrate.WonWithShowdownBeforeRakeBB,
                    winrate.WonWithoutShowdownBeforeRakeBB,
                },
                cancellationToken: cancellationToken
            )
        );
    }
}
