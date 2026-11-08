using PokerTrackerApi.HandImporting.Reconciliation;
using PokerTrackerApi.HandFileStorage;
using PokerTrackerApi.HandFileMetadata;

namespace PokerTrackerApi.HandImporting;

public static class HandImportingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandImporting(IConfiguration configuration)
        {
            services.AddHostedService<HandImportReconciliationService>();
            // TODO : move
            services.AddSingleton<IHandFileMetadataRepository, MySqlHandFileMetadataRepository>();
            services.AddScoped<IHandImportService, HandImportService>();

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

                cap.EnableSubscriberParallelExecute = true;
            });

            services.AddGarage(configuration);

            return services;
        }
    }
}
