using PokerTrackerApi.HandImporting.Reconciliation;
using PokerTrackerApi.HandImporting.Repository;
using PokerTrackerApi.HandFileStorage;

namespace PokerTrackerApi.HandImporting;

public static class HandImportingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandImporting(IConfiguration configuration)
        {
            services.AddHostedService<HandImportReconciliationService>();
            services.AddSingleton<IHandImportRepository, MySqlHandImportRepository>();
            services.AddScoped<IHandImportService, HandImportService>();
            services.AddScoped<IHandImportPersistenceService, HandImportPersistenceService>();

            services
                .AddOptions<HandImportReconciliationOptions>()
                .Bind(configuration.GetSection(HandImportReconciliationOptions.SectionName));

            services.AddSingleton(TimeProvider.System);

            var connectionString =
                configuration.GetConnectionString("PokerDatabase")
                ?? throw new InvalidOperationException(
                    "PokerDatabase connection string is missing."
                );
            var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"];
            if (string.IsNullOrWhiteSpace(kafkaBootstrapServers))
            {
                throw new InvalidOperationException(
                    "Kafka:BootstrapServers configuration is missing."
                );
            }

            services.AddCap(cap =>
            {
                cap.UseMySql(options => options.ConnectionString = connectionString);
                cap.UseKafka(options => options.Servers = kafkaBootstrapServers);
            });

            services.AddGarage(configuration);

            return services;
        }
    }
}
