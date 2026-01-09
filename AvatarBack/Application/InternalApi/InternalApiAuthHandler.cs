using System.Net;
using System.Net.Http.Headers;

namespace AvatarSentry.Application.InternalApi;

public class InternalApiAuthHandler : DelegatingHandler
{
    private readonly IInternalApiTokenService _tokenService;

    public InternalApiAuthHandler(IInternalApiTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokenService.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();
            _tokenService.InvalidateToken();

            var retryRequest = await CloneRequestAsync(request, cancellationToken);
            var retryToken = await _tokenService.GetTokenAsync(cancellationToken);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", retryToken);

            return await base.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(ct);
            var contentClone = new ByteArrayContent(contentBytes);

            foreach (var header in request.Content.Headers)
            {
                contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = contentClone;
        }

        return clone;
    }
}
