namespace Avatar_3D_Sentry.Settings;

public class AzureStorageOptions
{
    public const string SectionName = "AzureStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerNamePublic { get; set; } = "public";
    public string ContainerNameTts { get; set; } = "tts";

    public string BlobServiceEndpoint { get; set; } = string.Empty;

    // (Opcional) Si en tu .env tienes AzureStorage__AccountName y quieres usarlo:
    // public string AccountName { get; set; } = string.Empty;
}
