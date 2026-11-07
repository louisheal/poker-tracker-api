using Dapper;
using System.Data;

namespace PokerTrackerApi.HandImporting.Repository;

public class MySqlHandImportRepository : IHandImportRepository
{
    public async Task<Guid> CreateStartedAsync(
        string fileName,
        IDbConnection connection,
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
        IDbConnection connection,
        IDbTransaction transaction,
        CancellationToken cancellationToken
    )
    {
        const string query = """
                UPDATE HandFileMetadata
                SET Status = @Status
                WHERE FileId = @FileId
            """;

        var updatedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                query,
                new
                {
                    FileId = fileId,
                    Status = HandFileStatus.Stored,
                },
                transaction: transaction,
                cancellationToken: cancellationToken
            )
        );

        return updatedRows == 1;
    }

    public async Task MarkFailedAsync(
        Guid fileId,
        IDbConnection connection,
        CancellationToken cancellationToken
    )
    {
        const string query = """
                UPDATE HandFileMetadata
                SET Status = @Status
                WHERE FileId = @FileId;
            """;

        await connection.ExecuteAsync(
            new CommandDefinition(
                query,
                new
                {
                    FileId = fileId,
                    Status = HandFileStatus.Failed,
                },
                cancellationToken: cancellationToken
            )
        );
    }

    public async Task<IReadOnlyList<Guid>> GetIdsCreatedBeforeAsync(
        int limit,
        DateTimeOffset createdBefore,
        IDbConnection connection,
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
