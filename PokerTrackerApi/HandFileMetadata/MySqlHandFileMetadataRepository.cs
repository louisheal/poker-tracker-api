using System.Data;
using Dapper;
using PokerTrackerApi.Database;

namespace PokerTrackerApi.HandFileMetadata;

public class MySqlHandFileMetadataRepository : IHandFileMetadataRepository
{
    private readonly IPokerDatabaseConnectionFactory _connectionFactory;

    public MySqlHandFileMetadataRepository(IPokerDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreateMetadataAsync(
        string fileName,
        CancellationToken cancellationToken
    )
    {
        var fileId = Guid.NewGuid();

        const string query = """
                INSERT INTO HandFileMetadata
                    (FileId, FileName, Status, CreatedAt)
                VALUES
                    (@FileId, @FileName, @Status, @CreatedAt);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                query,
                new
                {
                    FileId = fileId,
                    FileName = fileName,
                    Status = HandFileStatus.Started,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                cancellationToken: cancellationToken
            )
        );

        return fileId;
    }

    public async Task<bool> MarkStoredAsync(
        Guid fileId,
        CancellationToken cancellationToken,
        IDbTransaction? transaction = null
    )
    {
        const string query = """
                UPDATE HandFileMetadata
                SET Status = @Status
                WHERE FileId = @FileId
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        var updatedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                query,
                new { FileId = fileId, Status = HandFileStatus.Stored },
                transaction: transaction,
                cancellationToken: cancellationToken
            )
        );

        return updatedRows == 1;
    }

    public async Task MarkFailedAsync(Guid fileId, CancellationToken cancellationToken)
    {
        const string query = """
                UPDATE HandFileMetadata
                SET Status = @Status
                WHERE FileId = @FileId;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                query,
                new { FileId = fileId, Status = HandFileStatus.Failed },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task MarkCompletedAsync(Guid fileId, CancellationToken cancellationToken)
    {
        const string query = """
                UPDATE HandFileMetadata
                SET Status = @Status
                WHERE FileId = @FileId;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                query,
                new { FileId = fileId, Status = HandFileStatus.Completed },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<IReadOnlyList<Guid>> GetIdsCreatedBeforeAsync(
        int limit,
        DateTimeOffset createdBefore,
        CancellationToken cancellationToken
    )
    {
        const string query = """
                SELECT FileId
                FROM HandFileMetadata
                WHERE Status = @Status
                  AND CreatedAt <= @CreatedBefore
                ORDER BY CreatedAt, FileId
                LIMIT @Limit;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        var fileIds = await connection.QueryAsync<string>(
            new CommandDefinition(
                query,
                new
                {
                    Status = HandFileStatus.Started,
                    CreatedBefore = createdBefore,
                    Limit = limit,
                },
                cancellationToken: cancellationToken
            )
        );

        return fileIds.Select(Guid.Parse).ToArray();
    }
}
