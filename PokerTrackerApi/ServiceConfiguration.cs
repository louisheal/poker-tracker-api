using System.Data.Common;
using MySqlConnector;
using Orleans.Configuration;

namespace PokerTrackerApi;

public static class ServiceConfiguration
{
    public static IServiceCollection AddCapMessaging(
        IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("PokerDatabase")
            ?? throw new InvalidOperationException("PokerDatabase connection string is missing.");
        var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"];
        if (string.IsNullOrWhiteSpace(kafkaBootstrapServers))
        {
            throw new InvalidOperationException("Kafka:BootstrapServers configuration is missing.");
        }

        services.AddCap(cap =>
        {
            cap.UseMySql(options => options.ConnectionString = connectionString);
            cap.UseKafka(options => options.Servers = kafkaBootstrapServers);
            cap.EnableSubscriberParallelExecute = true;
        });

        return services;
    }

    public static IHostBuilder UsePokerTrackerOrleans(
        IHostBuilder host,
        string connectionString
    )
    {
        DbProviderFactories.RegisterFactory(
            "MySql.Data.MySqlConnector",
            MySqlConnectorFactory.Instance
        );

        host.UseOrleans(siloBuilder =>
        {
            siloBuilder.UseLocalhostClustering();
            siloBuilder.Configure<ClusterOptions>(options =>
            {
                options.ClusterId = "poker-tracker-dev";
                options.ServiceId = "poker-tracker";
            });
            siloBuilder.AddAdoNetGrainStorage("Default", options =>
            {
                options.Invariant = "MySql.Data.MySqlConnector";
                options.ConnectionString = connectionString;
            });
        });

        return host;
    }
}
