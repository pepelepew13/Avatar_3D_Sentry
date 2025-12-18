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
                var exp = state.ExpiresAtUtc!.Value;
                if (exp.Kind == DateTimeKind.Local) exp = exp.ToUniversalTime();
                else if (exp.Kind == DateTimeKind.Unspecified) exp = DateTime.SpecifyKind(exp, DateTimeKind.Utc);

                var payload = new Persisted(
                    state.Token!,
                    exp,
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
        catch { /* silencioso */ }
    }

    public async Task TryRestoreAsync(AuthState state)
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", Key);
            if (string.IsNullOrWhiteSpace(json)) return;

            var p = JsonSerializer.Deserialize<Persisted>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (p is null) return;

            var exp = p.ExpiresAtUtc;
            if (exp.Kind == DateTimeKind.Local) exp = exp.ToUniversalTime();
            else if (exp.Kind == DateTimeKind.Unspecified) exp = DateTime.SpecifyKind(exp, DateTimeKind.Utc);

            if (exp <= DateTime.UtcNow)
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", Key);
                return;
            }

            state.SetSession(p.Token, exp, p.Email, p.Role, p.Empresa, p.Sede);
        }
        catch { /* silencioso */ }
    }

    public Task ClearAsync() => _js.InvokeVoidAsync("localStorage.removeItem", Key).AsTask();
}
