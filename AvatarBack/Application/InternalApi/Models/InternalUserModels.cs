namespace AvatarSentry.Application.InternalApi.Models;

/// <summary>Usuario en listados y GET /internal/users/{id} (sin PasswordHash).</summary>
public class InternalUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public string? CompanyName { get; set; }
    public string? SiteName { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>Respuesta de GET /internal/users/by-email/{email} (incluye PasswordHash para auth).</summary>
public class InternalUserByEmailDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? FullName { get; set; }
    public string Role { get; set; } = string.Empty;
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public bool IsActive { get; set; }
}

public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; } = new();
}

/// <summary>Filtro para GET /internal/users. company y site son IDs (int).</summary>
public class UserFilter
{
    public int? Company { get; set; }
    public int? Site { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Body para POST /internal/users (PascalCase).</summary>
public class CreateInternalUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Body para PUT /internal/users/{id}.</summary>
public class UpdateInternalUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public int? CompanyId { get; set; }
    public int? SiteId { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>Body para POST /internal/users/{id}/reset-password.</summary>
public class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
