namespace AbacusFileService.Settings;

public class AzureSettings
{
    public string? BlobContainer { get; set; }
    public string? ConnectionString { get; set; }
    
    public string? StorageAccountConnectionString { get; set; }
    
    public int BlobTokenExpiryInMinutes { get; set; }
}