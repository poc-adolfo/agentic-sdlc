using Microsoft.AspNetCore.Identity;

namespace AdminApi.Data.Entities;

/// <summary>
/// Tabela de junção <c>user_roles</c> (seção 3). Modelo permite múltiplos
/// papéis por usuário — a soma das permissões de todos os papéis atribuídos
/// constitui as permissões efetivas.
/// </summary>
public class UserRole : IdentityUserRole<Guid>
{
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public Guid GrantedBy { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
