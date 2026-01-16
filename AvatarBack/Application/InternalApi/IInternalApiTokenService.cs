namespace AvatarSentry.Application.InternalApi;

public interface IInternalApiTokenService
{
    Task<string> GetTokenAsync(CancellationToken ct = default);
    void InvalidateToken();
}
