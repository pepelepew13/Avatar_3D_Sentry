using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Avatar_3D_Sentry.Tests;

public class PruebasMiddlewareAutenticacionToken
{
    private const string Token = "secret-token";

    private static HttpClient ConstruirCliente()
    {
        var constructor = WebApplication.CreateBuilder();
        constructor.Configuration["TokenAutorizacion"] = Token;
        constructor.WebHost.UseTestServer();
        var app = constructor.Build();
        app.UseMiddleware<MiddlewareAutenticacionToken>();
        app.MapGet("/", () => "OK");
        app.RunAsync();
        return app.GetTestClient();
    }

    [Fact]
    public async Task Rechaza_Solicitud_Sin_Token()
    {
        var cliente = ConstruirCliente();
        var respuesta = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Permite_Solicitud_Con_Token_Valido()
    {
        var cliente = ConstruirCliente();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        var respuesta = await cliente.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}