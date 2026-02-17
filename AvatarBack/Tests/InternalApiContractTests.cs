using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AvatarBack.IntegrationTests;

/// <summary>
/// El BFF consume la API interna (documento MetaFusion→Sentry); esa API no está en este repo.
/// Con InternalApi:BaseUrl vacío el BFF arranca y usa stubs; con BaseUrl configurado consumiría /internal/*.
/// </summary>
public class InternalApiContractTests : IClassFixture<WebAppFactory>
{
    private readonly WebAppFactory _factory;

    public InternalApiContractTests(WebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BFF_Starts_WithoutInternalApiBaseUrl()
    {
        var client = _factory.CreateClient();
        // Ruta inexistente: el BFF responde 404. Confirma que la app arrancó (sin InternalApi:BaseUrl usa stubs).
        var response = await client.GetAsync("api/no-existe");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public class WebAppFactory : WebApplicationFactory<Avatar_3D_Sentry.ProgramEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Key"] = "test-jwt-key-minimum-32-chars-long!!"
            });
        });
    }
}
