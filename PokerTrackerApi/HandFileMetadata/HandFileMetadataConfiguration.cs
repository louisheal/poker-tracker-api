namespace PokerTrackerApi.HandFileMetadata;

public static class HandFileMetadataConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddHandFileMetadata()
        {
            services.AddSingleton<IHandFileMetadataRepository, MySqlHandFileMetadataRepository>();

            return services;
        }
    }
}
