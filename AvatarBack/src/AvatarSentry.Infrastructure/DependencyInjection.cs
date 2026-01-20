using AvatarSentry.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AvatarSentry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IInternalApiTokenService, InternalApiTokenService>();
        services.AddSingleton<IInternalUserClient, InternalUserClient>();
        services.AddSingleton<IInternalAvatarConfigClient, InternalAvatarConfigClient>();
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<ITtsService, TtsService>();

        return services;
    }
}
