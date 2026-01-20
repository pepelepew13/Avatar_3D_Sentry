namespace AvatarSentry.Application.Settings;

public class InternalApiSettings
{
    public const string SectionName = "InternalApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string AuthUser { get; set; } = string.Empty;
    public string AuthPassword { get; set; } = string.Empty;
}
