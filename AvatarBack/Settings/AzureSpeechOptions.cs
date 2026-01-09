namespace Avatar_3D_Sentry.Settings;

public class AzureSpeechOptions
{
    public const string SectionName = "AzureSpeech";

    public string SubscriptionKey { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    
    // Idioma por defecto (fallback)
    public string DefaultLanguage { get; set; } = "es";

    // Diccionario para mapear "es" -> "es-CO-SalomeNeural", "en" -> "en-US-AvaNeural", etc.
    public Dictionary<string, string> Voices { get; set; } = new();
}