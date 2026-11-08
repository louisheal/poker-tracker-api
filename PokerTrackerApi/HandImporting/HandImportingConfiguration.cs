using PokerTrackerApi.HandImporting.Reconciliation;

namespace PokerTrackerApi.HandImporting;

public static class HandImportingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandImporting(IConfiguration configuration)
        {
            services.AddHostedService<HandImportReconciliationService>();
            services.AddScoped<IHandImportService, HandImportService>();

            services
                .AddOptions<HandImportReconciliationOptions>()
                .Bind(configuration.GetSection(HandImportReconciliationOptions.SectionName));

            services.AddSingleton(TimeProvider.System);

            return services;
        }
    }
}
