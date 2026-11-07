using PokerTrackerApi.HandImporting.Reconciliation;
using PokerTrackerApi.HandImporting.Repository;
using PokerTrackerApi.HandImporting.Storage;

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

            services
                .AddOptions<HandImportReconciliationOptions>()
                .Bind(configuration.GetSection(HandImportReconciliationOptions.SectionName));

            services.AddSingleton(TimeProvider.System);

            services.AddGarage(configuration);

            return services;
        }
    }
}
