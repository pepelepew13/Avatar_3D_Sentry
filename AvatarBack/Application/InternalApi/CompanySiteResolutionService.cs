using AvatarSentry.Application.InternalApi.Clients;

namespace AvatarSentry.Application.InternalApi;

/// <summary>Resuelve códigos empresa/sede (string) a companyId/siteId (int) y viceversa usando la API interna.</summary>
public interface ICompanySiteResolutionService
{
    /// <summary>Resuelve código de empresa y opcionalmente sede a IDs. Devuelve null si no se encuentra.</summary>
    Task<(int CompanyId, int SiteId)?> ResolveToIdsAsync(string? empresaCode, string? sedeCode, CancellationToken ct = default);

    /// <summary>Obtiene nombres/códigos de empresa y sede para mostrar en el BFF (p. ej. claims JWT).</summary>
    Task<(string? CompanyName, string? SiteName)?> GetNamesAsync(int companyId, int siteId, CancellationToken ct = default);
}

public class CompanySiteResolutionService : ICompanySiteResolutionService
{
    private readonly IInternalCompanyClient _companyClient;
    private readonly IInternalSiteClient _siteClient;

    public CompanySiteResolutionService(IInternalCompanyClient companyClient, IInternalSiteClient siteClient)
    {
        _companyClient = companyClient;
        _siteClient = siteClient;
    }

    public async Task<(int CompanyId, int SiteId)?> ResolveToIdsAsync(string? empresaCode, string? sedeCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(empresaCode))
            return null;

        var company = await _companyClient.GetByCodeAsync(empresaCode!.Trim(), ct);
        if (company is null)
            return null;

        if (string.IsNullOrWhiteSpace(sedeCode))
        {
            var firstSite = await _siteClient.GetSitesAsync(company.Id, null, null, true, 1, 1, ct);
            var site = firstSite.Items?.FirstOrDefault();
            if (site is null)
                return null;
            return (company.Id, site.Id);
        }

        var siteByCode = await _siteClient.GetByCompanyAndCodeAsync(company.Id, sedeCode!.Trim(), ct);
        if (siteByCode is null)
            return null;

        return (company.Id, siteByCode.Id);
    }

    public async Task<(string? CompanyName, string? SiteName)?> GetNamesAsync(int companyId, int siteId, CancellationToken ct = default)
    {
        var company = await _companyClient.GetByIdAsync(companyId, ct);
        if (company is null)
            return null;
        // siteId 0 = solo empresa (p. ej. CompanyAdmin sin sede)
        if (siteId <= 0)
            return (company.Code ?? company.Name, null);
        var site = await _siteClient.GetByIdAsync(siteId, ct);
        if (site is null)
            return null;
        return (company.Code ?? company.Name, site.Code ?? site.Name);
    }
}
