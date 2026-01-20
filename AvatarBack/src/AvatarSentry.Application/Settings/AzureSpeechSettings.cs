namespace AvatarSentry.Application.Settings;

public class AzureSpeechSettings
{
    public const string SectionName = "AzureSpeech";

    public string SubscriptionKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string DefaultLanguage { get; set; } = "es";
    public VoiceSettings Voices { get; set; } = new();

    public class VoiceSettings
    {
        public string Es { get; set; } = string.Empty;
        public string En { get; set; } = string.Empty;
        public string Pt { get; set; } = string.Empty;
    }
}
