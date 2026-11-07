using Dapper;
using MySqlConnector;

namespace PokerTrackerApi.HandImporting.Repository;

public class MySqlHandImportRepository(MySqlDataSource dataSource) : IHandImportRepository
{
    public async Task<Guid> CreateStartedAsync(string fileName, CancellationToken cancellationToken)
    {
        var fileId = Guid.NewGuid();

        const string query = """
                INSERT INTO HandFileMetadata
                    (FileId, FileName, Status, CreatedAt)
                VALUES
                    (@FileId, @FileName, @Status, @CreatedAt);
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

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

    public Task MarkStored(Guid fileId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
