using AbacusFileService.Exceptions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AbacusFileService.Interfaces;
using AbacusFileService.Settings;
using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace AbacusFileService.Services
{
    public class FileService : IFileService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly AzureSettings _settings;

        public FileService(BlobServiceClient blobServiceClient, IOptions<AzureSettings> settings)
        {
            _settings = settings.Value;
            _blobServiceClient = blobServiceClient;
            _containerClient = _blobServiceClient.GetBlobContainerClient(_settings.BlobContainer);
        }

        /// <summary>
        /// Uploads a blob to the container. Throws FileExistsException if blob with the same name already exists.
        /// </summary>
        /// <param name="blobName">The name of the file</param>
        /// <param name="content">Stream content of the file</param>
        /// <param name="contentType">Optional content type</param>
        /// <param name="cancellationToken">Passed cancellation token to stop the upload</param>
        /// <exception cref="FileExistsException">When the file with the same name is already exists throws an exception</exception>
        public async Task UploadAsync(string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            if (await ExistsAsync(blobName, cancellationToken))
            {
                throw new FileExistsException($"File with name '{blobName}' already exists.");
            }
            var options = new BlobUploadOptions
            {
                HttpHeaders = contentType != null ? new BlobHttpHeaders { ContentType = contentType } : null
            };
            await blobClient.UploadAsync(content, options, cancellationToken);
        }
        
        /// <summary>
        /// Lists all the file names in the container. The names are URL encoded.
        /// </summary>
        /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
        /// <returns>Returns a list of strings</returns>
        public async Task<IEnumerable<string>> ListAsync(CancellationToken cancellationToken = default)
        {
            var names = new List<string>();
            
            await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                names.Add(Uri.EscapeDataString(blobItem.Name));
            }

            return names;
        }

        /// <summary>
        /// Returns a URL with SAS token to download the file. The token lifetime is configurable (by default its 15 minutes).
        /// Throws FileNotFoundException if blob does not exist.
        /// </summary>
        /// <param name="blobName">The name of the file</param>
        /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
        /// <returns>The url for the file with a SAS token to download.</returns>
        /// <exception cref="FileNotFoundException">If the file is not exists throws an exception</exception>
        public async Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync(cancellationToken);
            if(!exists.Value)
            {
                throw new FileNotFoundException($"File with name '{blobName}' does not exist.");
            }
            
            var blobUriBuilder = await CreateSasTokenBuilderAsync(blobName, blobClient, cancellationToken);

            return blobUriBuilder.ToUri().ToString();
        }

        /// <summary>
        /// Deletes a blob from the container. Returns true if the file is deleted or false if it's not exists (or already deleted).
        /// </summary>
        /// <param name="blobName">The name of the file</param>
        /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
        /// <returns>Returns true if the file is deleted or false if it's not exists (or already deleted).</returns>
        public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return response.Value;
        }
        
        /// <summary>
        /// Checks if a blob exists in the container.
        /// </summary>
        /// <param name="blobName">The name of the file</param>
        /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
        /// <returns>Returns true if its exists or false if not.</returns>
        private async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync(cancellationToken);
            return response.Value;
        }
        
        /// <summary>
        /// Creates a BlobUriBuilder with a SAS token for the blob.
        /// The expiry time is set based on the configuration (default 15 minutes).
        /// The start time is the current time (UTC).
        /// </summary>
        /// <param name="blobName">The name of the file</param>
        /// <param name="blobClient">The blobClient is required to get the Uri</param>
        /// <param name="cancellationToken">Passed cancellation token to stop the process</param>
        /// <returns>Returns a BlobUriBuilder</returns>
        private async Task<BlobUriBuilder> CreateSasTokenBuilderAsync(string blobName, BlobClient blobClient, CancellationToken cancellationToken = default)
        {
            var blobUriBuilder = new BlobUriBuilder(blobClient.Uri);
            //If the uri is already contains a SAS token, return it as is.
            if (blobUriBuilder.Sas != null)
            {
                return blobUriBuilder;
            }
            
            var accountKey = GetAccountKeyFromConnectionString(_settings.StorageAccountConnectionString);
            if (string.IsNullOrEmpty(accountKey))
            {
                throw new InvalidOperationException("Account key not found in connection string.");
            }
            
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_settings.BlobTokenExpiryInMinutes)
            };
    
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
            var credential = new StorageSharedKeyCredential(_blobServiceClient.AccountName, accountKey);
            blobUriBuilder.Sas = sasBuilder.ToSasQueryParameters(credential);

            return blobUriBuilder;
        }
        
        /// <summary>
        /// Extracts the AccountKey from the connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private string? GetAccountKeyFromConnectionString(string connectionString)
        {
            var parts = connectionString.Split(';');
            var accountKeyPart = parts.FirstOrDefault(p => p.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase));
            return accountKeyPart?.Substring("AccountKey=".Length);
        }
    }
}
