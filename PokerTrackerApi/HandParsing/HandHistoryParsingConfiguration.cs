namespace PokerTrackerApi.HandParsing;

public static class HandHistoryParsingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandParsing()
        {
            services.AddSingleton<IHandHistoryParser, GgPokerHandHistoryParser>();
            services.AddTransient<RawHandUpdateReceiver>();

            return services;
        }
    }
}
