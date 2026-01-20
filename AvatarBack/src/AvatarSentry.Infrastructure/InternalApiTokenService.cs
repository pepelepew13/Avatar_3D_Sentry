using AvatarSentry.Application.Interfaces;

namespace AvatarSentry.Infrastructure;

public class InternalApiTokenService : IInternalApiTokenService
{
    public Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(string.Empty);
    }

    public Task InvalidateAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
