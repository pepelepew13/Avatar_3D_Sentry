using System.Text.Json;
using Microsoft.JSInterop;

namespace AvatarAdmin.Services;

public sealed class AuthPersistence
{
    private readonly IJSRuntime _js;
    private const string Key = "avataradmin.auth";

    public AuthPersistence(IJSRuntime js) => _js = js;

    private sealed record Persisted(
        string Token,
        DateTime ExpiresAtUtc,
        string Email,
        string Role,
        string? Empresa,
        string? Sede
    );

    public async Task PersistAsync(AuthState state)
    {
        try
        {
            if (state.IsAuthenticated && !string.IsNullOrWhiteSpace(state.Token) && state.ExpiresAtUtc is not null)
            {
                var payload = new Persisted(
                    state.Token!,
                    state.ExpiresAtUtc!.Value,
                    state.Email,
                    state.Role,
                    state.Empresa,
                    state.Sede
                );

                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                await _js.InvokeVoidAsync("localStorage.setItem", Key, json);
            }
            else
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", Key);
            }
        }
        catch { /* no romper UI si storage falla */ }
    }

    public async Task TryRestoreAsync(AuthState state)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (string.IsNullOrWhiteSpace(json)) return;

            var p = JsonSerializer.Deserialize<Persisted>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (p is null) return;

            if (p.ExpiresAtUtc <= DateTime.UtcNow)
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", Key);
                return;
            }

            state.SetSession(p.Token, p.ExpiresAtUtc, p.Email, p.Role, p.Empresa, p.Sede);
        }
        catch { /* silencioso */ }
    }

    public Task ClearAsync() => _js.InvokeVoidAsync("localStorage.removeItem", Key).AsTask();
}
