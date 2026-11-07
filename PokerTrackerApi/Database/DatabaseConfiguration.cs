using MySqlConnector;

namespace PokerTrackerApi.Database;

public static class DatabaseConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDatabase(string connectionString)
        {
            var dataSource = new MySqlDataSource(connectionString);
            services.AddSingleton(dataSource);

            return services;
        }
    }
}
