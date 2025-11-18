using AbacusFileService.Settings;
using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;

namespace AbacusFileService.Extensions;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Adds configuration for AzureSettings from appsettings and environment variables.
    /// Adds AZURE_STORAGE_CONNECTION_STRING and AZURE_STORAGE_CONTAINER_NAME environment variables support.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind from appsettings (if present)
        services.Configure<AzureSettings>(configuration.GetSection("AzureSettings"));

        // Override from environment variables if present
        var envConn = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
        var envContainer = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONTAINER_NAME");
        
        if (!string.IsNullOrWhiteSpace(envConn) || !string.IsNullOrWhiteSpace(envContainer))
        {
            services.PostConfigure<AzureSettings>(s =>
            {
                if (!string.IsNullOrWhiteSpace(envConn))
                    s.StorageAccountConnectionString = envConn;
                if (!string.IsNullOrWhiteSpace(envContainer))
                    s.BlobContainer = envContainer;
            });
        }

        return services;
    }
    
    /// <summary>
    /// Adds the required Azure Services to the IServiceCollection.
    /// The current implementation adds BlobServiceClient and using the DefaultAzureCredential for authentication.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException">If AzureSettings is not found in the configuration or the ConnectionString is missing.</exception>
    public static IServiceCollection AddAzureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureClients(clientBuilder =>
        {
            clientBuilder.AddClient<BlobServiceClient, BlobClientOptions>((options, sp) =>
            {
                var azureSettings = sp.GetRequiredService<IOptions<AzureSettings>>().Value;

                if (string.IsNullOrWhiteSpace(azureSettings.StorageAccountConnectionString))
                    throw new ApplicationException("Azure Storage Connection String is not configured.");
                
                if (string.IsNullOrWhiteSpace(azureSettings.BlobContainer))
                    throw new ApplicationException("Azure Blob Container name is not configured.");
                
                return new BlobServiceClient(azureSettings.StorageAccountConnectionString.Trim(), options);
            });
        });

        return services;
    }
}