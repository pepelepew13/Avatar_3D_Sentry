using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace AvatarAdmin.Services;

public sealed class BearerHandler : DelegatingHandler
{
    private readonly AuthState _auth;
    private readonly NavigationManager _nav;

    public BearerHandler(AuthState auth, NavigationManager nav)
    {
        _auth = auth;
        _nav = nav;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Espera breve a que la sesión se restaure (si aplica)
        await _auth.EnsureHydratedAsync(ct);

        var path = request.RequestUri?.AbsolutePath ?? "";
        var isAuthEndpoint = path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase);

        if (!isAuthEndpoint && !string.IsNullOrWhiteSpace(_auth.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }

        if (!isAuthEndpoint && _auth.IsExpired && !string.IsNullOrWhiteSpace(_auth.Token))
        {
            _auth.Logout("expired");
            if (TryGetCurrentRelative(out var currentRel))
            {
                _nav.NavigateTo($"/auth?returnUrl={Uri.EscapeDataString(currentRel)}", replace: true);
            }

            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                ReasonPhrase = "Expired local session"
            };
        }

        var resp = await base.SendAsync(request, ct);

        if (resp.StatusCode == HttpStatusCode.Unauthorized && !isAuthEndpoint)
        {
            _auth.Logout("unauthorized");
            if (TryGetCurrentRelative(out var currentRel))
            {
                _nav.NavigateTo($"/auth?returnUrl={Uri.EscapeDataString(currentRel)}", replace: true);
            }
        }

        return resp;
    }

    private bool TryGetCurrentRelative(out string relPath)
    {
        try
        {
            var abs = _nav.Uri;
            var rel = _nav.ToBaseRelativePath(abs);
            relPath = "/" + rel;
            if (string.IsNullOrEmpty(rel)) relPath = "/";
            return true;
        }
        catch (InvalidOperationException)
        {
            relPath = "/";
            return false;
        }
    }
}
