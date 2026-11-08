namespace PokerTrackerApi.HandFileSplitting;

public static class HandFileSplittingConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandFileSplitting()
        {
            services.AddSingleton<IHandFileSplitter, GgPokerHandFileSplitter>();
            services.AddTransient<HandFileUpdatesReceiver>();

            return services;
        }
    }
}
