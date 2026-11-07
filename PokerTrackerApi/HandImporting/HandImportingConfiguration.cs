using PokerTrackerApi.HandImporting.Repository;
using PokerTrackerApi.HandImporting.Storage;

namespace PokerTrackerApi.HandImporting;

public static class HandImportingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandImporting(IConfiguration configuration)
        {
            services.AddScoped<IHandImportRepository, MySqlHandImportRepository>();
            services.AddScoped<IHandImportService, HandImportService>();

            services.AddGarage(configuration);

            return services;
        }
    }
}
