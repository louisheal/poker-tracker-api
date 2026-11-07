using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace PokerTrackerApi.HandImporting.Storage;

public static class GarageConfiguration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGarage(IConfiguration configuration)
        {
            services
                .AddOptions<HandImportStorageOptions>()
                .Bind(configuration.GetSection(HandImportStorageOptions.SectionName));

            services.AddSingleton<IAmazonS3>(serviceProvider =>
            {
                var storage = serviceProvider
                    .GetRequiredService<IOptions<HandImportStorageOptions>>()
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

            services.AddSingleton<IHandFileStore, GarageHandFileStore>();

            return services;
        }
    }
}
