using Amazon.S3;
using Amazon.S3.Model;

namespace PokerTrackerApi.HandImporting.Storage;

public sealed class GarageHandFileStore : IHandFileStore
{
    private const string BucketName = "poker-tracker";
    private readonly IAmazonS3 _s3Client;

    public GarageHandFileStore(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }

    public async Task SaveAsync(Stream file, Guid fileId, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = BucketName,
            Key = $"hand-files/{fileId:N}.txt",
            InputStream = file,
            UseChunkEncoding = false,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }
}
