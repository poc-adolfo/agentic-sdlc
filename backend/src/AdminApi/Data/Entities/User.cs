using Microsoft.AspNetCore.Identity;

namespace AdminApi.Data.Entities;

/// <summary>
/// Tabela <c>users</c> da seção 3 da spec.
/// Herda de IdentityUser (armazena PasswordHash, Email confirmado, lockout,
/// etc.) e adiciona os campos de domínio próprios da spec.
/// </summary>
public class User : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;

    /// <summary>active | disabled. Soft-delete (nunca exclusão física).</summary>
    public UserStatus Status { get; set; } = UserStatus.Active;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public List<UserRole> UserRoles { get; set; } = new();
}

public enum UserStatus { Active, Disabled }
