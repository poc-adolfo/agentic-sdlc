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
    public const string RolesManage = "roles:manage";
    public const string PermissionsAssign = "permissions:assign";
    public const string RolesAssign = "roles:assign";
    public const string UsersList = "users:list";
    public const string UsersView = "users:view";

    /// <summary>Todas as permissões do catálogo, usado para registrar policies dinamicamente.</summary>
    public static readonly string[] All =
    {
        UsersCreate, UsersEdit, UsersDisable,
        RolesManage, PermissionsAssign, RolesAssign,
        UsersList, UsersView,
    };

    /// <summary>Descrição humana de cada permissão, usada no seed da tabela permissions.</summary>
    public static readonly Dictionary<string, string> Descriptions = new()
    {
        [UsersCreate] = "Criar usuários",
        [UsersEdit] = "Editar dados de usuários e atribuir papéis",
        [UsersDisable] = "Desativar/reativar usuários (soft-delete)",
        [RolesManage] = "Criar, editar e excluir papéis",
        [PermissionsAssign] = "Compor permissões de um papel",
        [RolesAssign] = "Atribuir papéis a usuários",
        [UsersList] = "Listar e buscar usuários",
        [UsersView] = "Ver detalhes de um usuário",
    };
}
