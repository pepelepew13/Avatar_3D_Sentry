using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Services;
using AvatarSentry.Application.InternalApi;
using AvatarSentry.Application.InternalApi.Models;

namespace AvatarSentry.Application.InternalApi.Clients;

/// <summary>
/// Stubs cuando InternalApi:BaseUrl no está configurado.
/// La API que conecta la DB con el sistema (documento MetaFusion→Sentry) la proporciona Sentry; hasta entonces el BFF no puede atender login ni gestión users/avatar-config.
/// </summary>
internal static class InternalApiNotConfigured
{
    public const string Message =
        "La API interna no está configurada (InternalApi:BaseUrl vacío). " +
        "Según el documento técnico, la API que conecta la DB con el sistema la proporciona el equipo Sentry.";
}

public class StubInternalUserClient : IInternalUserClient
{
    public Task<PagedResponse<InternalUserDto>> GetUsersAsync(UserFilter filter, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserByEmailDto?> GetByEmailAsync(string email, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserDto> CreateAsync(CreateInternalUserRequest request, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalUserDto> UpdateAsync(int id, UpdateInternalUserRequest request, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task ResetPasswordAsync(int id, ResetPasswordRequest request, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubInternalAvatarConfigClient : IInternalAvatarConfigClient
{
    public Task<PagedResponse<InternalAvatarConfigDto>> GetConfigsAsync(AvatarConfigFilter filter, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto?> GetByScopeAsync(int? company, int? site, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto> CreateAsync(CreateInternalAvatarConfigRequest request, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalAvatarConfigDto> UpdateAsync(int id, UpdateInternalAvatarConfigRequest request, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubCompanySiteResolutionService : ICompanySiteResolutionService
{
    public Task<(int CompanyId, int SiteId)?> ResolveToIdsAsync(string? empresaCode, string? sedeCode, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<(string? CompanyName, string? SiteName)?> GetNamesAsync(int companyId, int siteId, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubInternalCompanyClient : IInternalCompanyClient
{
    public Task<InternalCompanyDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalCompanyDto?> GetByCodeAsync(string code, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<PagedResponse<InternalCompanyDto>> GetCompaniesAsync(string? code, string? name, string? sector, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubInternalSiteClient : IInternalSiteClient
{
    public Task<InternalSiteDto?> GetByIdAsync(int id, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<InternalSiteDto?> GetByCompanyAndCodeAsync(int companyId, string code, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<PagedResponse<InternalSiteDto>> GetSitesAsync(int? companyId, string? code, string? name, bool? isActive, int page = 1, int pageSize = 10, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubInternalKpisClient : IInternalKpisClient
{
    public Task<string?> GetGlobalAsync(CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<string?> GetByCompanyAsync(int companyId, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<string?> GetBySiteAsync(int siteId, CancellationToken ct = default)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}

public class StubAvatarDataStore : IAvatarDataStore
{
    public Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<ApplicationUser?> FindUserByIdAsync(int id, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<(int total, List<ApplicationUser> items)> ListUsersAsync(int skip, int take, string? q, string? role, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<bool> UserEmailExistsAsync(string email, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<ApplicationUser> CreateUserAsync(ApplicationUser user, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task UpdateUserAsync(ApplicationUser user, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteUserAsync(ApplicationUser user, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task UpdateUserPasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<AvatarConfig?> FindAvatarConfigAsync(string empresa, string sede, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task<AvatarConfig> CreateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task UpdateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
    public Task DeleteAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
        => throw new InvalidOperationException(InternalApiNotConfigured.Message);
}
