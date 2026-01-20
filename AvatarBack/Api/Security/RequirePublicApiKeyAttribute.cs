using System;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Security;

public sealed class RequirePublicApiKeyAttribute : TypeFilterAttribute
{
    public RequirePublicApiKeyAttribute() : base(typeof(RequirePublicApiKeyFilter))
    {
    }
}

public class RequirePublicApiKeyFilter : IAsyncActionFilter
{
    private const string HeaderName = "X-Api-Key";
    private readonly PublicApiOptions _options;

    public RequirePublicApiKeyFilter(IOptions<PublicApiOptions> options)
    {
        _options = options.Value;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            await next();
            return;
        }

        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var providedKey) ||
            !string.Equals(providedKey.ToString(), _options.ApiKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "API key inválida." });
            return;
        }

        await next();
    }
}
