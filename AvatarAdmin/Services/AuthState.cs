using System.Security.Claims;

namespace AvatarAdmin.Services;

public sealed class AuthState
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public string? Token { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    public string Email { get; private set; } = string.Empty;
    public string Role  { get; private set; } = string.Empty;
    public string? Empresa { get; private set; }
    public string? Sede    { get; private set; }

    public string? LastLogoutReason { get; private set; }
    public DateTime? LastLogoutAtUtc { get; private set; }

    // ✅ Propiedad de conveniencia que faltaba
    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    // ===== Hidratación =====
    private readonly TaskCompletionSource _hydratedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool IsHydrated => _hydratedTcs.Task.IsCompleted;
    public async Task EnsureHydratedAsync(CancellationToken ct = default)
    {
        try { await _hydratedTcs.Task.WaitAsync(TimeSpan.FromSeconds(2), ct); }
        catch (TimeoutException) { /* continuar */ }
    }
    public void MarkHydrated()
    {
        if (!IsHydrated) _hydratedTcs.TrySetResult();
        Changed?.Invoke();
    }

    public bool IsExpired =>
        (ExpiresAtUtc ?? DateTime.MinValue) <= DateTime.UtcNow.Subtract(ClockSkew);

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(Token) && !IsExpired;

    public TimeSpan? TimeRemaining() =>
        ExpiresAtUtc is null ? null : (ExpiresAtUtc.Value - DateTime.UtcNow);

    public event Action? Changed;

    public void SetSession(string token, DateTime expiresAtUtc, string email, string role, string? empresa, string? sede)
    {
        Token = token;
        ExpiresAtUtc = expiresAtUtc.Kind switch
        {
            DateTimeKind.Local => expiresAtUtc.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(expiresAtUtc, DateTimeKind.Utc),
            _ => expiresAtUtc
        };

        Email = email ?? string.Empty;
        Role  = role  ?? string.Empty;
        Empresa = empresa;
        Sede = sede;

        LastLogoutReason = null;
        LastLogoutAtUtc = null;

        MarkHydrated();
        Changed?.Invoke();
    }

    public void Logout(string? reason = "manual")
    {
        Token = null;
        ExpiresAtUtc = null;
        Email = string.Empty;
        Role  = string.Empty;
        Empresa = null;
        Sede = null;

        LastLogoutReason = reason;
        LastLogoutAtUtc = DateTime.UtcNow;

        MarkHydrated();
        Changed?.Invoke();
    }
}
