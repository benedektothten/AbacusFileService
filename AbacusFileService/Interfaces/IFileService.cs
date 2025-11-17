namespace AbacusFileService.Interfaces;

public interface IFileService
{
    /// <summary>
    /// Uploads a blob to the container. Throws FileExistsException if blob with the same name already exists.
    /// </summary>
    /// <param name="blobName">The name of the file</param>
    /// <param name="content">Stream content of the file</param>
    /// <param name="contentType">Optional content type</param>
    /// <param name="cancellationToken">Passed cancellation token to stop the upload</param>
    Task UploadAsync(string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Lists all the file names in the container. The names are URL encoded.
    /// </summary>
    /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
    /// <returns>Returns a list of strings</returns>
    Task<IEnumerable<string>> ListAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a URL with SAS token to download the file. The token lifetime is configurable (by default its 15 minutes).
    /// Throws FileNotFoundException if blob does not exist.
    /// </summary>
    /// <param name="blobName">The name of the file</param>
    /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
    /// <returns>The url for the file with a SAS token to download.</returns>
    Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a blob from the container. Returns true if the file is deleted or false if it's not exists (or already deleted).
    /// </summary>
    /// <param name="blobName">The name of the file</param>
    /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
    /// <returns>Returns true if the file is deleted or false if it's not exists (or already deleted).</returns>
    Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    

}