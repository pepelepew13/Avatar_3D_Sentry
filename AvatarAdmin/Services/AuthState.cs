using System.Security.Claims;

namespace AvatarAdmin.Services;

public sealed class AuthState
{
    public string? Token { get; private set; }
    public DateTime? ExpiresAtUtc { get; private set; }

    public string Email { get; private set; } = string.Empty;
    public string Role  { get; private set; } = string.Empty;
    public string? Empresa { get; private set; }
    public string? Sede    { get; private set; }

    // Motivo del último cierre de sesión para mostrar un toast
    // "expired" | "unauthorized" | "manual"
    public string? LastLogoutReason { get; private set; }
    public DateTime? LastLogoutAtUtc { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(Token) && (ExpiresAtUtc ?? DateTime.MinValue) > DateTime.UtcNow;

    public bool IsAdmin => string.Equals(Role, "Admin", StringComparison.OrdinalIgnoreCase);

    public bool IsExpired => (ExpiresAtUtc ?? DateTime.MinValue) <= DateTime.UtcNow;

    public TimeSpan? TimeRemaining() =>
        ExpiresAtUtc is null ? null : ExpiresAtUtc.Value - DateTime.UtcNow;

    // 🔔 Notificación de cambios para que los componentes refresquen su UI.
    public event Action? Changed;

    public void SetSession(string token, DateTime expiresAtUtc, string email, string role, string? empresa, string? sede)
    {
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        Email = email ?? string.Empty;
        Role  = role  ?? string.Empty;
        Empresa = empresa;
        Sede = sede;

        // Limpiamos el motivo anterior (si lo hubiera)
        LastLogoutReason = null;
        LastLogoutAtUtc = null;

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

        Changed?.Invoke();
    }
}
