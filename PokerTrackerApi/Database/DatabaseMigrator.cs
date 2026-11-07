using DbUp;

namespace PokerTrackerApi.Database;

public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        var upgrader = DeployChanges
            .To.MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(DatabaseMigrator).Assembly,
                name => name.Contains(".Migrations.", StringComparison.Ordinal)
            )
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            throw new InvalidOperationException("Database migration failed.", result.Error);
        }
    }
}
