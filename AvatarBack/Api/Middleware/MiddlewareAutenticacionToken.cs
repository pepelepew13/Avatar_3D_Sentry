using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Avatar_3D_Sentry.Middleware;

public class MiddlewareAutenticacionToken
{
    private readonly RequestDelegate _siguiente;
    private readonly string? _token;

    public MiddlewareAutenticacionToken(RequestDelegate siguiente, IConfiguration configuracion)
    {
        _siguiente = siguiente;
        _token = configuracion["TokenAutorizacion"];
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        if (string.IsNullOrEmpty(_token) ||
            !contexto.Request.Headers.TryGetValue("Authorization", out var encabezado))
        {
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await contexto.Response.WriteAsync("No autorizado");
            return;
        }

        var valor = encabezado.ToString();
        if (!valor.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(_token, valor.Substring("Bearer ".Length)))
        {
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await contexto.Response.WriteAsync("No autorizado");
            return;
        }

        await _siguiente(contexto);
    }
}