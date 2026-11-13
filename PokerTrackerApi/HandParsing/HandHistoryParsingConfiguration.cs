using PokerTrackerApi.HandParsing.Repository;

namespace PokerTrackerApi.HandParsing;

public static class HandHistoryParsingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandParsing()
        {
            services.AddSingleton<IHandHistoryRepository, MySqlHandHistoryRepository>();
            services.AddSingleton<IHandHistoryParser, GgPokerHandHistoryParser>();
            services.AddTransient<RawHandUpdateReceiver>();

            return services;
        }
    }
}
