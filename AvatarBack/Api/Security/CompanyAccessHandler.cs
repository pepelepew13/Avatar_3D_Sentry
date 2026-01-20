using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Avatar_3D_Sentry.Security;

public class CompanyAccessHandler : AuthorizationHandler<CompanyAccessRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyAccessRequirement requirement)
    {
        // SuperAdmin o Admin: acceso completo
        var role = context.User.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Usuario: comparar claims vs route data (empresa/sede)
        var empresaClaim = context.User.FindFirst("empresa");
        var sedeClaim    = context.User.FindFirst("sede");

        if (empresaClaim is null) return Task.CompletedTask;

        var empresaRoute = GetRouteValue(context, "empresa");
        var sedeRoute    = GetRouteValue(context, "sede");

        var okEmpresa = empresaRoute is null
            ? true
            : string.Equals(empresaClaim.Value, empresaRoute, StringComparison.OrdinalIgnoreCase);

        if (!okEmpresa) return Task.CompletedTask;

        if (!string.IsNullOrWhiteSpace(sedeClaim?.Value))
        {
            if (sedeRoute is null)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if (!string.Equals(sedeClaim!.Value, sedeRoute, StringComparison.OrdinalIgnoreCase))
                return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private static string? GetRouteValue(AuthorizationHandlerContext ctx, string key)
    {
        if (ctx.Resource is not HttpContext http) return null;
        if (!http.Request.RouteValues.TryGetValue(key, out var value)) return null;
        return value?.ToString()?.Trim().ToLowerInvariant();
    }
}
