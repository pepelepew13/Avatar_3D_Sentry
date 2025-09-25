using System;

namespace Avatar_3D_Sentry.Services;

public class UnsupportedLanguageException : Exception
{
    public UnsupportedLanguageException(string language)
        : base($"El idioma '{language}' no está soportado.")
    {
        Language = language;
    }

    public string Language { get; }
}
