using System.Text.Json;
using Dapper;
using PokerTrackerApi.Database;
using PokerTrackerApi.Domain.HandHistories;
using PokerTrackerApi.Domain.HandHistories.Events;

namespace PokerTrackerApi.HandParsing.Repository;

public class MySqlHandHistoryRepository : IHandHistoryRepository
{
    private readonly IPokerDatabaseConnectionFactory _connectionFactory;

    public MySqlHandHistoryRepository(IPokerDatabaseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task SaveHandHistory(
        HandHistory handHistory,
        CancellationToken cancellationToken
    )
    {
        const string saveHandQuery = """
                INSERT INTO HandHistory
                    (
                        HandId,
                        HeroPlayerId,
                        HeroHoleCardsJson,
                        Timestamp,
                        TotalPotBB,
                        RakeBB
                    )
                VALUES
                    (
                        @HandId,
                        @HeroPlayerId,
                        @HeroHoleCardsJson,
                        @Timestamp,
                        @TotalPotBB,
                        @RakeBB
                    )
                ON DUPLICATE KEY UPDATE
                    HeroPlayerId = VALUES(HeroPlayerId),
                    HeroHoleCardsJson = VALUES(HeroHoleCardsJson),
                    Timestamp = VALUES(Timestamp),
                    TotalPotBB = VALUES(TotalPotBB),
                    RakeBB = VALUES(RakeBB);
            """;

        const string deletePlayersQuery = """
                DELETE FROM HandHistoryPlayer
                WHERE HandId = @HandId;
            """;

        const string deleteEventsQuery = """
                DELETE FROM HandHistoryEvent
                WHERE HandId = @HandId;
            """;

        const string savePlayersQuery = """
                INSERT INTO HandHistoryPlayer
                    (HandId, PlayerId, Position, StartingStackBB)
                VALUES
                    (@HandId, @PlayerId, @Position, @StartingStackBB);
            """;

        const string saveEventsQuery = """
                INSERT INTO HandHistoryEvent
                    (HandId, Sequence, EventType, PayloadJson)
                VALUES
                    (@HandId, @Sequence, @EventType, @PayloadJson);
            """;

        var handDao = new HandHistoryDao
        {
            HandId = handHistory.HandId,
            HeroPlayerId = handHistory.HeroPlayerId,
            HeroHoleCardsJson = JsonSerializer.Serialize(handHistory.HeroHoleCards),
            Timestamp = handHistory.Timestamp,
            TotalPotBB = handHistory.TotalPotBB,
            RakeBB = handHistory.RakeBB,
        };
        var playerDaos = handHistory
            .Players.Select(player => new HandHistoryPlayerDao
            {
                HandId = handHistory.HandId,
                PlayerId = player.PlayerId,
                Position = player.Position,
                StartingStackBB = player.StartingStackBB,
            })
            .ToArray();
        var eventDaos = handHistory
            .Events.Select(handHistoryEvent =>
                CreateEventDao(handHistory.HandId, handHistoryEvent)
            )
            .ToArray();

        await using var connection = await _connectionFactory.OpenConnectionAsync(
            cancellationToken
        );
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                saveHandQuery,
                handDao,
                transaction,
                cancellationToken: cancellationToken
            )
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                deletePlayersQuery,
                new { handHistory.HandId },
                transaction,
                cancellationToken: cancellationToken
            )
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                deleteEventsQuery,
                new { handHistory.HandId },
                transaction,
                cancellationToken: cancellationToken
            )
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                savePlayersQuery,
                playerDaos,
                transaction,
                cancellationToken: cancellationToken
            )
        );
        await connection.ExecuteAsync(
            new CommandDefinition(
                saveEventsQuery,
                eventDaos,
                transaction,
                cancellationToken: cancellationToken
            )
        );

        await transaction.CommitAsync(cancellationToken);
    }

    private static HandHistoryEventDao CreateEventDao(
        string handId,
        HandHistoryEvent handHistoryEvent
    )
    {
        var payloadJson = JsonSerializer.Serialize<HandHistoryEvent>(handHistoryEvent);
        using var payloadDocument = JsonDocument.Parse(payloadJson);

        return new HandHistoryEventDao
        {
            HandId = handId,
            Sequence = handHistoryEvent.Sequence,
            EventType = payloadDocument.RootElement.GetProperty("eventType").GetString()
                ?? throw new InvalidOperationException(
                    $"Event type discriminator is missing for {handHistoryEvent.GetType().Name}."
                ),
            PayloadJson = payloadJson,
        };
    }
}
