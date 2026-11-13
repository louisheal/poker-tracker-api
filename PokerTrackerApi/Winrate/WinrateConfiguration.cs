using PokerTrackerApi.Winrate.Repository;

namespace PokerTrackerApi.Winrate;

public static class WinrateConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddWinrate()
        {
            services.AddSingleton<IWinrateRepository, MySqlWinrateRepository>();
            services.AddTransient<IWinrateService, WinrateService>();
            services.AddTransient<HandHistoryUpdateReceiver>();

            return services;
        }
    }
}
