namespace Avatar_3D_Sentry.Settings;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerNamePublic { get; set; } = "public";
    public string ContainerNameTts { get; set; } = "tts";
    public string ContainerNameVideos { get; set; } = "public";

    public string BlobServiceEndpoint { get; set; } = string.Empty;

    /// <summary>Minutos de validez de las URLs SAS generadas. Por defecto 10.</summary>
    public int SasExpiryMinutes { get; set; } = 10;

    // (Opcional) Si en tu .env tienes AzureStorage__AccountName y quieres usarlo:
    // public string AccountName { get; set; } = string.Empty;
}
