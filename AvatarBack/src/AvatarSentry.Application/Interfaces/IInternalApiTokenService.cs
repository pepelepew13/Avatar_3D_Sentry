namespace AvatarSentry.Application.Interfaces;

public interface IInternalApiTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken);
    Task InvalidateAsync(CancellationToken cancellationToken);
}
