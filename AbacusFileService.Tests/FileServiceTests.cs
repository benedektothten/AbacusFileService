using AbacusFileService.Exceptions;
using AbacusFileService.Services;
using AbacusFileService.Settings;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using Moq;

namespace AbacusFileService.Tests;

public class FileServiceTests
{
    private readonly Mock<BlobServiceClient> _blobServiceClientMock = new();
    private readonly Mock<BlobContainerClient> _containerClientMock = new();
    private readonly Mock<BlobClient> _blobClientMock = new();
    private readonly AzureSettings _settings = new()
    {
        BlobContainer = "test-container",
        StorageAccountConnectionString = "DefaultEndpointsProtocol=https;AccountName=testAccountName;AccountKey=YXBwbGVib3lzZW5keW91dGhlYmVzdHdpc2hlc2Zvcm1lcnJ5Y2hyaXN0bWFzYW5kaGFwcHluZXd5ZWFyMjAyNQ==;EndpointSuffix=core.windows.net",
        BlobTokenExpiryInMinutes = 15
    };

    private FileService CreateService()
    {
        _blobServiceClientMock
            .Setup(x => x.GetBlobContainerClient(_settings.BlobContainer))
            .Returns(_containerClientMock.Object);

        _containerClientMock
            .Setup(x => x.CreateIfNotExists(It.IsAny<PublicAccessType>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .Returns(Response.FromValue<BlobContainerInfo>(default!, Mock.Of<Response>()));

        // Remove the GetParentBlobServiceClient setup

        var options = Options.Create(_settings);
        return new FileService(_blobServiceClientMock.Object, options);
    }

    [Fact]
    public async Task UploadAsync_WhenBlobDoesNotExist_UploadsBlob()
    {
        // arrange
        var service = CreateService();
        var blobName = "file.txt";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _containerClientMock
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(_blobClientMock.Object);

        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        _blobClientMock
            .Setup(x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue<BlobContentInfo>(default!, Mock.Of<Response>()));

        // act
        await service.UploadAsync(blobName, stream, "text/plain", CancellationToken.None);

        // assert
        _blobClientMock.Verify(
            x => x.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UploadAsync_WhenBlobExists_ThrowsFileExistsException()
    {
        // arrange
        var service = CreateService();
        var blobName = "file.txt";
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _containerClientMock
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(_blobClientMock.Object);

        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // act \& assert
        await Assert.ThrowsAsync<FileExistsException>(() =>
            service.UploadAsync(blobName, stream, "text/plain", CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_WhenBlobDoesNotExist_ThrowsFileNotFoundException()
    {
        // arrange
        var service = CreateService();
        var blobName = "missing.txt";

        _containerClientMock
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(_blobClientMock.Object);

        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        // act \& assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.DownloadAsync(blobName, CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_WhenBlobExists_ReturnsSasUrl()
    {
        // arrange
        var service = CreateService();
        var blobName = "file.txt";

        _containerClientMock
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(_blobClientMock.Object);

        _blobClientMock
            .Setup(x => x.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Configure the same BlobServiceClient that FileService was built with
        var delegationKey = BlobsModelFactory.UserDelegationKey(
            "oid",
            "tid",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(_settings.BlobTokenExpiryInMinutes),
            signedService: "b",
            signedVersion: "2020-10-02",
            value: Convert.ToBase64String(new byte[32]));

        _blobServiceClientMock
            .Setup(x => x.GetUserDelegationKeyAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(delegationKey, Mock.Of<Response>()));

        var blobUri = new Uri("https://account.blob.core.windows.net/container/file.txt");
        _blobClientMock
            .Setup(x => x.Uri)
            .Returns(blobUri);

        // act
        var url = await service.DownloadAsync(blobName, CancellationToken.None);

        // assert
        Assert.NotNull(url);
        Assert.Contains("https://account.blob.core.windows.net/container/file.txt", url);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrueWhenBlobDeleted()
    {
        // arrange
        var service = CreateService();
        var blobName = "file.txt";

        _containerClientMock
            .Setup(x => x.GetBlobClient(blobName))
            .Returns(_blobClientMock.Object);

        _blobClientMock
            .Setup(x => x.DeleteIfExistsAsync(
                DeleteSnapshotsOption.None,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // act
        var result = await service.DeleteAsync(blobName, CancellationToken.None);

        // assert
        Assert.True(result);
    }

    private static AsyncPageable<BlobItem> GetAsyncPageable(IReadOnlyList<BlobItem> items)
    {
        IEnumerable<Page<BlobItem>> GetPages()
        {
            yield return Page<BlobItem>.FromValues(items, null, Mock.Of<Response>());
        }

        return AsyncPageable<BlobItem>.FromPages(GetPages());
    }
}
