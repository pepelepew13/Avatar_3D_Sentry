using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Controllers;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Avatar_3D_Sentry.Tests;

public class AvatarControllerTests
{
    [Fact]
    public async Task Anunciar_PortugueseLanguage_ReturnsOk()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        var solicitud = new SolicitudAnuncio
        {
            Empresa = "Empresa",
            Sede = "Lisboa",
            Modulo = "B1",
            Turno = "Tarde",
            Nombre = "Ana"
        };

        var result = await controller.Anunciar("pt", solicitud);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AnnouncementResponse>(okResult.Value);
        Assert.Equal("Empresa", response.Empresa);
        Assert.Equal("Lisboa", response.Sede);
        Assert.Contains("Ana", response.Texto);
        Assert.StartsWith("data:audio/mpeg;base64,", response.AudioUrl);
    }

    [Fact]
    public async Task Anunciar_UnsupportedLanguage_ReturnsBadRequest()
    {
        using var context = CreateContext();
        var controller = CreateController(context);
        var solicitud = new SolicitudAnuncio
        {
            Empresa = "Empresa",
            Sede = "Madrid",
            Modulo = "M1",
            Turno = "Mañana",
            Nombre = "Luis"
        };

        var result = await controller.Anunciar("fr", solicitud);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No hay plantillas disponibles para el idioma fr.", badRequest.Value);
    }

    [Theory]
    [InlineData("es")]
    [InlineData("ES")]
    [InlineData("Es")]
    public async Task Anunciar_SpanishVariants_UseSameVoice(string idioma)
    {
        using var context = CreateContext();
        var fakeTts = new FakeTtsService();
        var controller = CreateController(context, fakeTts);
        var solicitud = new SolicitudAnuncio
        {
            Empresa = "Empresa",
            Sede = "Madrid",
            Modulo = "M1",
            Turno = "Mañana",
            Nombre = "Luis"
        };

        var result = await controller.Anunciar(idioma, solicitud);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<AnnouncementResponse>(okResult.Value);
        Assert.Equal("es", fakeTts.LastIdioma);
        Assert.Equal("Lucia", fakeTts.LastVoz);
    }

    private static AvatarController CreateController(AvatarContext context, ITtsService? tts = null)
    {
        var generator = new PhraseGenerator();
        tts ??= new FakeTtsService();
        return new AvatarController(generator, tts, context);
    }

    private static AvatarContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AvatarContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AvatarContext(options);
    }

    private sealed class FakeTtsService : ITtsService
    {
        private static readonly IReadOnlyDictionary<string, List<string>> Voices = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = new() { "Lucia" },
            ["en"] = new() { "Joanna" },
            ["pt"] = new() { "Camila" }
        };

        public string? LastIdioma { get; private set; }
        public string? LastVoz { get; private set; }

        public IReadOnlyDictionary<string, List<string>> GetAvailableVoices() => Voices;

        public Task<TtsResultado> SynthesizeAsync(string texto, string idioma, string voz)
        {
            LastIdioma = idioma;
            LastVoz = voz;

            if (!Voices.TryGetValue(idioma, out var voces) || !voces.Contains(voz))
            {
                throw new ArgumentException($"Voz no soportada: {voz} para idioma {idioma}", nameof(voz));
            }

            return Task.FromResult(new TtsResultado
            {
                Audio = new byte[] { 1, 2, 3 },
                Visemas = new List<Visema>
                {
                    new Visema { ShapeKey = "A", Tiempo = 0 }
                }
            });
        }
    }
}
