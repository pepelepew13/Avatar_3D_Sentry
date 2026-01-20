namespace Avatar_3D_Sentry.Settings;

public class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AuthUser { get; set; } = string.Empty;
    public string AuthPassword { get; set; } = string.Empty;
}
