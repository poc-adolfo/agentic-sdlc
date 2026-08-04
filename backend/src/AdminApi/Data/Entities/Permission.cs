namespace AdminApi.Data.Entities;

/// <summary>
/// Tabela <c>permissions</c> da seção 3. Catálogo fixo de permissões,
/// populado em código (não editável pela UI). Formato recurso:ação.
/// </summary>
public class Permission
{
    /// <summary>Ex: "users:create", "roles:manage". Usado como PK.</summary>
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<RolePermission> RolePermissions { get; set; } = new();
}
