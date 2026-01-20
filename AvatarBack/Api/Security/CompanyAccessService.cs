using System.Security.Claims;

namespace Avatar_3D_Sentry.Security;

public sealed class CompanyAccessService : ICompanyAccessService
{
    public bool CanAccess(ClaimsPrincipal user, string empresa, string? sede)
    {
        if (user?.Identity?.IsAuthenticated != true) return false;

        var role = user.FindFirstValue(ClaimTypes.Role);
        if (string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var empresaClaim = user.FindFirst("empresa")?.Value;
        if (string.IsNullOrWhiteSpace(empresaClaim)) return false;

        if (!string.Equals(empresaClaim.Trim(), empresa?.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        var sedeClaim = user.FindFirst("sede")?.Value;
        if (string.IsNullOrWhiteSpace(sedeClaim)) return true;

        if (string.IsNullOrWhiteSpace(sede)) return true;

        return string.Equals(sedeClaim.Trim(), sede.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
