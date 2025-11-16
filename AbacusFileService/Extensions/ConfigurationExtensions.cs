using AbacusFileService.Settings;
using Azure.Identity;
using Microsoft.Extensions.Azure;

namespace AbacusFileService.Extensions;

public static class ConfigurationExtensions
{
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

            clientBuilder.AddBlobServiceClient(new Uri(azureSettings.StorageAccountConnectionString));

            DefaultAzureCredential credential = new();
            clientBuilder.UseCredential(credential);
        });

        return services;
    }
}