using AbacusFileService.Settings;
using Azure.Identity;
using Microsoft.Extensions.Azure;

namespace AbacusFileService.Extensions;

public static class ConfigurationExtensions
{
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
        services.Configure<AzureSettings>(configuration.GetSection("AzureSettings"));

        services.AddAzureClients(clientBuilder =>
        {
            var azureSettings = configuration.GetSection("AzureSettings").Get<AzureSettings>();
            if (azureSettings == null)
            {
                throw new ApplicationException("Azure Settings not found");
            }

            if (azureSettings.ConnectionString == null)
            {
                throw new ApplicationException("Azure Storage Connection String is not configured.");
            }

            clientBuilder.AddBlobServiceClient(new Uri(azureSettings.StorageAccountConnectionString));

            DefaultAzureCredential credential = new();
            clientBuilder.UseCredential(credential);
        });

        return services;
    }
}