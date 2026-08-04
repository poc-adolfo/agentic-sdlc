using Microsoft.AspNetCore.Identity;

namespace AdminApi.Data.Entities;

/// <summary>
/// Tabela <c>roles</c> da seção 3 da spec.
/// <c>IsSystem</c> marca papéis criados no bootstrap (ex: Administrador raiz)
/// que não podem ser excluídos — só editados nas suas permissões.
/// </summary>
public class Role : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
    public bool IsSystem { get; set; }

    public List<RolePermission> RolePermissions { get; set; } = new();
    public List<UserRole> UserRoles { get; set; } = new();
}
