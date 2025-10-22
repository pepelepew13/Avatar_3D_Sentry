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
        var path = request.RequestUri?.AbsolutePath ?? "";
        var isAuthEndpoint = path.Contains("/api/auth/login", StringComparison.OrdinalIgnoreCase);

        // Adjunta bearer si procede
        if (!isAuthEndpoint && !string.IsNullOrWhiteSpace(_auth.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);
        }

        // Sesión local expirada: corta pronto y redirige a /auth (si es posible)
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

        // 401 del backend => cierra sesión y manda a /auth con returnUrl relativo
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
            var abs = _nav.Uri;                     // absoluto actual
            var rel = _nav.ToBaseRelativePath(abs); // relativo: "" o "editor?..."
            relPath = "/" + rel;
            if (string.IsNullOrEmpty(rel)) relPath = "/";
            return true;
        }
        catch (InvalidOperationException)
        {
            // RemoteNavigationManager no inicializado (p.ej. prerender)
            relPath = "/";
            return false;
        }
    }
}
