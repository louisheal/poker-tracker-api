using PokerTrackerApi.HandImporting.Repository;

namespace PokerTrackerApi.HandImporting;

public static class HandImportingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandImporting()
        {
            services.AddScoped<IHandImportRepository, MySqlHandImportRepository>();
            services.AddScoped<IHandImportService, HandImportService>();
            return services;
        }
    }
}
