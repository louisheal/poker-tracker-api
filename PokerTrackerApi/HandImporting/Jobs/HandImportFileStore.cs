namespace PokerTrackerApi.HandImporting.Jobs;

public record StagedHandImportFile(Guid FileId, int Sequence, string FileName, string StorageKey);

public interface IHandImportFileStore
{
    Task<IReadOnlyList<StagedHandImportFile>> StageAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken
    );

    Stream OpenRead(string storageKey);

    void Delete(string storageKey);
}

public class HandImportFileStore : IHandImportFileStore
{
    private readonly string _directory;

    public HandImportFileStore(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
    }

    public async Task<IReadOnlyList<StagedHandImportFile>> StageAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        var stagedFiles = new List<StagedHandImportFile>(files.Count);

        try
        {
            for (var index = 0; index < files.Count; index++)
            {
                var upload = files[index];
                var fileId = Guid.NewGuid();
                var storageKey = fileId.ToString("N");
                var fileName = Path.GetFileName(upload.FileName.Replace('\\', '/'));
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "upload.txt";
                }
                if (fileName.Length > 255)
                {
                    fileName = fileName[..255];
                }

                stagedFiles.Add(new StagedHandImportFile(fileId, index, fileName, storageKey));
                await using var output = new FileStream(
                    GetPath(storageKey),
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true
                );
                await upload.CopyToAsync(output, cancellationToken);
            }

            return stagedFiles;
        }
        catch
        {
            foreach (var file in stagedFiles)
            {
                Delete(file.StorageKey);
            }

            throw;
        }
    }

    public Stream OpenRead(string storageKey) =>
        new FileStream(
            GetPath(storageKey),
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true
        );

    public void Delete(string storageKey)
    {
        var path = GetPath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string GetPath(string storageKey)
    {
        if (!Guid.TryParseExact(storageKey, "N", out _))
        {
            throw new ArgumentException("The staged file key is invalid.", nameof(storageKey));
        }

        return Path.Combine(_directory, storageKey);
    }
}
