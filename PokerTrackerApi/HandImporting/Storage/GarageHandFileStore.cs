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
            Key = GetObjectKey(fileId),
            InputStream = file,
            UseChunkEncoding = false,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }

    public async Task RemoveFileAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = BucketName,
            Key = GetObjectKey(fileId),
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    private static string GetObjectKey(Guid fileId) => $"hand-files/{fileId:N}.txt";
}
