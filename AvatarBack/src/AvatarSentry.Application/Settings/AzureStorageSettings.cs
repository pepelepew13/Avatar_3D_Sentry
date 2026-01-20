namespace AvatarSentry.Application.Settings;

public class AzureStorageSettings
{
    public const string SectionName = "AzureStorage";

    public string ConnectionString { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string ContainerNamePublic { get; set; } = "public";
    public string ContainerNameTts { get; set; } = "tts";
    public string BlobServiceEndpoint { get; set; } = string.Empty;
}
