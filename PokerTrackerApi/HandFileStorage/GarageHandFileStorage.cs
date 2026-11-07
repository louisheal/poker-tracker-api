using Amazon.S3;
using Amazon.S3.Model;

namespace PokerTrackerApi.HandFileStorage;

public sealed class GarageHandFileStorage : IHandFileStorage
{
    private const string BucketName = "poker-tracker";
    private readonly IAmazonS3 _s3Client;

    public GarageHandFileStorage(IAmazonS3 s3Client)
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

    public async Task<Stream> OpenReadAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var request = new GetObjectRequest { BucketName = BucketName, Key = GetObjectKey(fileId) };
        var response = await _s3Client.GetObjectAsync(request, cancellationToken);

        return response.ResponseStream;
    }

    public async Task RemoveAsync(Guid fileId, CancellationToken cancellationToken)
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
