using AbacusFileService.Exceptions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AbacusFileService.Interfaces;
using AbacusFileService.Settings;
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
            _containerClient = blobServiceClient.GetBlobContainerClient(_settings.BlobContainer);
            _containerClient.CreateIfNotExists(PublicAccessType.None);
            _blobServiceClient = blobServiceClient;
        }

        public async Task UploadAsync(string blobName, Stream content, string? contentType = null, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            if (await ExistsAsync(blobName, cancellationToken))
            {
                throw new FileExistsException($"Blob with name '{blobName}' already exists.");
            }
            var options = new BlobUploadOptions
            {
                HttpHeaders = contentType != null ? new BlobHttpHeaders { ContentType = contentType } : null
            };
            await blobClient.UploadAsync(content, options, cancellationToken).ConfigureAwait(false);
        }
        
        public async Task<IEnumerable<string>> ListAsync(CancellationToken cancellationToken = default)
        {
            var names = new List<string>();

            await foreach (var blobItem in _containerClient.GetBlobsAsync(cancellationToken: cancellationToken))
            {
                names.Add(Uri.EscapeDataString(blobItem.Name));
            }

            return names;
        }

        public async Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var exists = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            if(!exists.Value)
            {
                throw new FileNotFoundException($"File with name '{blobName}' does not exist.");
            }
            
            var blobUriBuilder = await CreateSasTokenBuilder(blobName, cancellationToken, blobClient);

            return blobUriBuilder.ToUri().ToString();
        }

        public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return response.Value;
        }
        
        private async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            var response = await blobClient.ExistsAsync(cancellationToken).ConfigureAwait(false);
            return response.Value;
        }
        
        private async Task<BlobUriBuilder> CreateSasTokenBuilder(string blobName, CancellationToken cancellationToken, BlobClient blobClient)
        {
            var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(
                startsOn: DateTimeOffset.UtcNow,
                expiresOn: DateTimeOffset.UtcNow.AddMinutes(_settings.BlobTokenExpiryInMinutes),
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_settings.BlobTokenExpiryInMinutes)
            };
    
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
            {
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _blobServiceClient.AccountName)
            };
            
            return blobUriBuilder;
        }
    }
}
