using Avatar_3D_Sentry.Modelos;
using Avatar_3D_Sentry.Models;
using Avatar_3D_Sentry.Settings;
using AvatarSentry.Application.InternalApi;
using AvatarSentry.Application.InternalApi.Clients;
using AvatarSentry.Application.InternalApi.Models;
using Microsoft.Extensions.Options;

namespace Avatar_3D_Sentry.Services;

public class InternalApiAvatarDataStore : IAvatarDataStore
{
    private readonly InternalApiOptions _options;
    private readonly HttpClient _httpClient;
    private readonly IInternalUserClient _userClient;
    private readonly IInternalAvatarConfigClient _avatarConfigClient;
    private readonly ICompanySiteResolutionService _resolution;
    private readonly Uri _baseUri;

    public InternalApiAvatarDataStore(
        IOptions<InternalApiOptions> options,
        HttpClient httpClient,
        IInternalUserClient userClient,
        IInternalAvatarConfigClient avatarConfigClient,
        ICompanySiteResolutionService resolution)
    {
        _options = options.Value;
        _httpClient = httpClient;
        _userClient = userClient;
        _avatarConfigClient = avatarConfigClient;
        _resolution = resolution;
        _baseUri = BuildBaseUri(_options.BaseUrl);
    }

    public async Task<ApplicationUser?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        var dto = await _userClient.GetByEmailAsync(email, ct);
        if (dto is null) return null;
        return MapByEmailToUser(dto);
    }

    public async Task<ApplicationUser?> FindUserByIdAsync(int id, CancellationToken ct)
    {
        var dto = await _userClient.GetByIdAsync(id, ct);
        if (dto is null) return null;
        return MapToUser(dto);
    }

    public async Task<(int total, List<ApplicationUser> items)> ListUsersAsync(int skip, int take, string? q, string? role, CancellationToken ct)
    {
        var page = Math.Max(1, (skip / Math.Max(1, take)) + 1);
        var pageSize = Math.Max(1, take);
        var filter = new UserFilter
        {
            Company = null,
            Site = null,
            Email = q,
            Role = role,
            Page = page,
            PageSize = pageSize
        };
        var result = await _userClient.GetUsersAsync(filter, ct);
        var items = (result.Items ?? new List<InternalUserDto>()).Select(MapToUser).ToList();
        return (result.Total, items);
    }

    public async Task<bool> UserEmailExistsAsync(string email, CancellationToken ct)
    {
        var user = await FindUserByEmailAsync(email, ct);
        return user is not null;
    }

    public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        var ids = await _resolution.ResolveToIdsAsync(user.Empresa, user.Sede, ct);
        var payload = new CreateInternalUserRequest
        {
            Email = user.Email,
            Password = user.PasswordHash,
            Role = user.Role,
            FullName = user.Email,
            CompanyId = ids?.CompanyId,
            SiteId = ids?.SiteId,
            IsActive = user.IsActive
        };
        var created = await _userClient.CreateAsync(payload, ct);
        return MapToUser(created);
    }

    public async Task UpdateUserAsync(ApplicationUser user, CancellationToken ct)
    {
        var ids = await _resolution.ResolveToIdsAsync(user.Empresa, user.Sede, ct);
        var payload = new UpdateInternalUserRequest
        {
            Email = user.Email,
            Password = null,
            Role = user.Role,
            FullName = user.Email,
            CompanyId = ids?.CompanyId,
            SiteId = ids?.SiteId,
            IsActive = user.IsActive
        };
        await _userClient.UpdateAsync(user.Id, payload, ct);
    }

    public async Task DeleteUserAsync(ApplicationUser user, CancellationToken ct)
    {
        await _userClient.DeleteAsync(user.Id, ct);
    }

    public async Task UpdateUserPasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
    {
        await _userClient.ResetPasswordAsync(userId, new ResetPasswordRequest { NewPassword = passwordHash }, ct);
    }

    public async Task<AvatarConfig?> FindAvatarConfigAsync(string empresa, string sede, CancellationToken ct)
    {
        var ids = await _resolution.ResolveToIdsAsync(empresa, sede, ct);
        if (!ids.HasValue) return null;
        var dto = await _avatarConfigClient.GetByScopeAsync(ids.Value.CompanyId, ids.Value.SiteId, ct);
        if (dto is null) return null;
        return MapToAvatarConfig(dto, empresa, sede);
    }

    public async Task<AvatarConfig> CreateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        var ids = await _resolution.ResolveToIdsAsync(config.Empresa, config.Sede, ct);
        if (!ids.HasValue)
            throw new InvalidOperationException($"No se pudieron resolver empresa/sede: {config.Empresa}/{config.Sede}");
        var request = new CreateInternalAvatarConfigRequest
        {
            CompanyId = ids.Value.CompanyId,
            SiteId = ids.Value.SiteId,
            ModelPath = config.Vestimenta,
            BackgroundPath = config.Fondo ?? config.BackgroundPath,
            LogoPath = config.LogoPath,
            Language = config.Idioma,
            HairColor = config.ColorCabello,
            VoiceIds = Array.Empty<int>(),
            Status = "Draft",
            IsActive = config.IsActive
        };
        var created = await _avatarConfigClient.CreateAsync(request, ct);
        return MapToAvatarConfig(created, config.Empresa, config.Sede);
    }

    public async Task UpdateAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        var ids = await _resolution.ResolveToIdsAsync(config.Empresa, config.Sede, ct);
        if (!ids.HasValue)
            throw new InvalidOperationException($"No se pudieron resolver empresa/sede: {config.Empresa}/{config.Sede}");
        var request = new UpdateInternalAvatarConfigRequest
        {
            CompanyId = ids.Value.CompanyId,
            SiteId = ids.Value.SiteId,
            ModelPath = config.Vestimenta,
            BackgroundPath = config.Fondo ?? config.BackgroundPath,
            LogoPath = config.LogoPath,
            Language = config.Idioma,
            HairColor = config.ColorCabello,
            VoiceIds = Array.Empty<int>(),
            Status = "Draft",
            IsActive = config.IsActive
        };
        await _avatarConfigClient.UpdateAsync(config.Id, request, ct);
    }

    public async Task DeleteAvatarConfigAsync(AvatarConfig config, CancellationToken ct)
    {
        await _avatarConfigClient.DeleteAsync(config.Id, ct);
    }

    private static ApplicationUser MapByEmailToUser(InternalUserByEmailDto dto)
    {
        return new ApplicationUser
        {
            Id = dto.Id,
            Email = dto.Email,
            PasswordHash = dto.PasswordHash ?? string.Empty,
            Role = dto.Role,
            Empresa = null,
            Sede = null,
            IsActive = dto.IsActive
        };
    }

    private static ApplicationUser MapToUser(InternalUserDto dto)
    {
        return new ApplicationUser
        {
            Id = dto.Id,
            Email = dto.Email,
            PasswordHash = string.Empty,
            Role = dto.Role,
            Empresa = dto.CompanyName,
            Sede = dto.SiteName,
            IsActive = dto.IsActive
        };
    }

    private static AvatarConfig MapToAvatarConfig(InternalAvatarConfigDto dto, string empresa, string sede)
    {
        return new AvatarConfig
        {
            Id = dto.Id,
            Empresa = empresa,
            Sede = sede,
            Vestimenta = dto.ModelUrl,
            Fondo = dto.BackgroundUrl,
            BackgroundPath = dto.BackgroundUrl,
            LogoPath = dto.LogoUrl,
            Idioma = dto.Language,
            ColorCabello = dto.HairColor,
            IsActive = dto.IsActive
        };
    }

    private Uri BuildAbsoluteUri(string relativeOrAbsolute)
    {
        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out var absolute))
            return absolute;
        return new Uri(_baseUri, relativeOrAbsolute);
    }

    private static Uri BuildBaseUri(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Falta InternalApi:BaseUrl para consumir la API interna.");
        var normalized = baseUrl.Trim().TrimEnd('/') + "/";
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"InternalApi:BaseUrl inválido: {baseUrl}");
        return uri;
    }
}
