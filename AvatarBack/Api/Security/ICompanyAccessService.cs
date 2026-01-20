using System.Security.Claims;

namespace Avatar_3D_Sentry.Security;

public interface ICompanyAccessService
{
    bool CanAccess(ClaimsPrincipal user, string empresa, string? sede);
}
