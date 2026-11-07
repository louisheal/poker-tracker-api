using Amazon.S3;
using Amazon.S3.Model;

namespace PokerTrackerApi.HandImporting;

public sealed class GarageHandFileStore : IHandFileStore
{
    private const string BucketName = "hand-files";
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
            Key = $"{fileId:N}.txt",
            InputStream = file,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);
    }
}
