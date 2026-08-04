namespace AdminApi.Data.Entities;

/// <summary>
/// Tabela de junção <c>role_permissions</c> (seção 3). Aqui mora a
/// granularidade: um admin compõe, por papel, quais permissões do catálogo
/// esse papel carrega.
/// </summary>
public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public string PermissionId { get; set; } = string.Empty;
    public Permission Permission { get; set; } = null!;
}
