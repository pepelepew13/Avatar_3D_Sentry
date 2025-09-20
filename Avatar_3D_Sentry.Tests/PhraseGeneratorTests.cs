using Avatar_3D_Sentry.Services;
using System.Collections.Generic;
using Xunit;

namespace Avatar_3D_Sentry.Tests;

public class PhraseGeneratorTests
{
    [Fact]
    public void Generate_ReplacesAllFields_Spanish()
    {
        var generator = new PhraseGenerator();
        var fields = new Dictionary<string, string>
        {
            ["empresa"] = "Empresa",
            ["sede"] = "Sede",
            ["modulo"] = "M1",
            ["turno"] = "T1",
            ["nombre"] = "Juan"
        };

        var result = generator.Generate("es", fields);
        foreach (var field in fields.Values)
        {
            Assert.Contains(field, result);
        }
    }

    [Fact]
    public void Generate_ReplacesAllFields_English()
    {
        var generator = new PhraseGenerator();
        var fields = new Dictionary<string, string>
        {
            ["empresa"] = "Company",
            ["sede"] = "HQ",
            ["modulo"] = "Module A",
            ["turno"] = "Morning",
            ["nombre"] = "John"
        };

        var result = generator.Generate("en", fields);
        foreach (var field in fields.Values)
        {
            Assert.Contains(field, result);
        }
    }

    [Fact]
    public void Generate_ReplacesAllFields_Portuguese()
    {
        var generator = new PhraseGenerator();
        var fields = new Dictionary<string, string>
        {
            ["empresa"] = "Empresa",
            ["sede"] = "Lisboa",
            ["modulo"] = "B1",
            ["turno"] = "Tarde",
            ["nombre"] = "Ana"
        };

        var result = generator.Generate("pt", fields);
        foreach (var field in fields.Values)
        {
            Assert.Contains(field, result);
        }
    }

    [Fact]
    public void Generate_InvalidLanguage_ThrowsUnsupportedLanguageException()
    {
        var generator = new PhraseGenerator();
        var fields = new Dictionary<string, string>
        {
            ["empresa"] = "Empresa",
            ["sede"] = "Sede",
            ["modulo"] = "M1",
            ["turno"] = "T1",
            ["nombre"] = "Luis"
        };

        var exception = Assert.Throws<UnsupportedLanguageException>(() => generator.Generate("fr", fields));

        Assert.Equal("El idioma 'fr' no está soportado.", exception.Message);
    }
}
