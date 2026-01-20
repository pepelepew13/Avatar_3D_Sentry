using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avatar_3D_Sentry.Settings;
using Microsoft.Extensions.Options;

namespace AvatarSentry.Application.InternalApi;

public class InternalApiApiKeyHandler : DelegatingHandler
{
    private readonly InternalApiOptions _opt;

    public InternalApiApiKeyHandler(IOptions<InternalApiOptions> opt)
    {
        _opt = opt.Value;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
        {
            // Evita duplicados si el request se reintenta
            request.Headers.Remove("X-Api-Key");
            request.Headers.TryAddWithoutValidation("X-Api-Key", _opt.ApiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
