using System.Text.Json.Serialization;

namespace AvatarAdmin.Models;

public sealed class LoginRequest
{
    [JsonPropertyName("email")]    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    [JsonPropertyName("token")]        public string Token { get; set; } = string.Empty;
    [JsonPropertyName("role")]         public string Role { get; set; } = string.Empty;
    [JsonPropertyName("empresa")]      public string? Empresa { get; set; }
    [JsonPropertyName("sede")]         public string? Sede { get; set; }
    [JsonPropertyName("expiresAtUtc")] public DateTime ExpiresAtUtc { get; set; }
}

/// <summary>
/// Solo para SUPERADMIN en el panel (crea usuarios en el backend).
/// Role debe ser "Admin" o "User".
/// Si Role="Admin", Empresa y Sede se ignoran.
/// </summary>
public sealed class CreateUserRequest
{
    [JsonPropertyName("email")]    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
    [JsonPropertyName("role")]     public string Role { get; set; } = "User";
    [JsonPropertyName("empresa")]  public string? Empresa { get; set; }
    [JsonPropertyName("sede")]     public string? Sede { get; set; }
    [JsonPropertyName("isActive")] public bool IsActive { get; set; } = true;
}
