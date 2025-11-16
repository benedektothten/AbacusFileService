namespace AbacusFileService.Interfaces;

public interface IFileService
{
    Task UploadAsync(string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default);
    Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> ListAsync(CancellationToken cancellationToken = default);
}