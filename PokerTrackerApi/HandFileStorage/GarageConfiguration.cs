using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace PokerTrackerApi.HandFileStorage;

public static class GarageConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGarage(IConfiguration configuration)
        {
            services
                .AddOptions<HandFileStorageOptions>()
                .Bind(configuration.GetSection(HandFileStorageOptions.SectionName));

            services.AddSingleton<IAmazonS3>(serviceProvider =>
            {
                var storage = serviceProvider
                    .GetRequiredService<IOptions<HandFileStorageOptions>>()
                    .Value;
                var s3Config = new AmazonS3Config
                {
                    ServiceURL = storage.Endpoint,
                    ForcePathStyle = true,
                    AuthenticationRegion = storage.Region
                };
                var credentials = new BasicAWSCredentials(
                    storage.AccessKey,
                    storage.SecretKey
                );

                return new AmazonS3Client(credentials, s3Config);
            });

            services.AddSingleton<IHandFileStorage, GarageHandFileStorage>();

            return services;
        }
    }
}
