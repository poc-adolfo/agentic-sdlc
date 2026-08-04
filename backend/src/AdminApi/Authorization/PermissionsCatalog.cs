namespace AdminApi.Authorization;

/// <summary>
/// Catálogo fixo de permissões (seção 3 da spec). Definido em código —
/// novo recurso/ação = nova entrada aqui, não via UI de admin.
/// Formato <c>recurso:ação</c>.
/// </summary>
public static class PermissionsCatalog
{
    public const string UsersCreate = "users:create";
    public const string UsersEdit = "users:edit";
    public const string UsersDisable = "users:disable";
    public const string UsersList = "users:list";
    public const string UsersView = "users:view";
    public const string RolesManage = "roles:manage";
    public const string RolesList = "roles:list";
    public const string RolesView = "roles:view";
    public const string PermissionsAssign = "permissions:assign";
    public const string RolesAssign = "roles:assign";

    /// <summary>Todas as permissões do catálogo, usado para registrar policies dinamicamente.</summary>
    public static readonly string[] All =
    {
        UsersCreate, UsersEdit, UsersDisable, UsersList, UsersView,
        RolesManage, RolesList, RolesView,
        PermissionsAssign, RolesAssign,
    };

    /// <summary>Descrição humana de cada permissão, usada no seed da tabela permissions.</summary>
    public static readonly Dictionary<string, string> Descriptions = new()
    {
        [UsersCreate] = "Criar usuários",
        [UsersEdit] = "Editar dados de usuários e atribuir papéis",
        [UsersDisable] = "Desativar/reativar usuários (soft-delete)",
        [UsersList] = "Listar e buscar usuários",
        [UsersView] = "Ver detalhes de um usuário",
        [RolesManage] = "Criar, editar e excluir papéis",
        [RolesList] = "Listar e buscar papéis",
        [RolesView] = "Ver detalhes de um papel",
        [PermissionsAssign] = "Compor permissões de um papel",
        [RolesAssign] = "Atribuir papéis a usuários",
    };
}
