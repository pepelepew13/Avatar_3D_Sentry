using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Data;
using Avatar_3D_Sentry.Modelos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Avatar_3D_Sentry.Tests;

public class AvatarControllerIntegrationTests : IClassFixture<AvatarApplicationFactory>
{
    private readonly AvatarApplicationFactory _factory;

    public AvatarControllerIntegrationTests(AvatarApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anunciar_SinCredenciales_ReturnsServiceUnavailable()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/avatar/anunciar?idioma=es")
        {
            Content = new StringContent(JsonSerializer.Serialize(new SolicitudAnuncio
            {
                Empresa = "Empresa",
                Sede = "Sede",
                Modulo = "M1",
                Turno = "Mañana",
                Nombre = "Persona"
            }), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AvatarApplicationFactory.TokenAutorizacion);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Configura las credenciales de AWS Polly", body, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class AvatarApplicationFactory : WebApplicationFactory<Program>
{
    public const string TokenAutorizacion = "integration-token";

    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["ConnectionStrings:AvatarDatabase"] = "Data Source=integration-tests.db",
                ["TokenAutorizacion"] = TokenAutorizacion,
                ["AWS:AccessKeyId"] = string.Empty,
                ["AWS:SecretAccessKey"] = string.Empty
            };

            configurationBuilder.AddInMemoryCollection(overrides!);
        });

        builder.ConfigureTestServices(services =>
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var descriptor = services.SingleOrDefault(s => s.ServiceType == typeof(DbContextOptions<AvatarContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AvatarContext>(options => options.UseSqlite(_connection));

            var provider = services.BuildServiceProvider();
            try
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AvatarContext>();
                db.Database.EnsureCreated();
            }
            finally
            {
                provider.Dispose();
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
        }
    }

}
