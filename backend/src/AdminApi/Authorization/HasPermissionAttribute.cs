using Microsoft.AspNetCore.Authorization;

namespace AdminApi.Authorization;

/// <summary>
/// Alias semântico para <c>[Authorize(Policy = "...")]</c> baseado em
/// permissão do catálogo. Uso: <c>[HasPermission(PermissionsCatalog.UsersCreate)]</c>.
/// </summary>
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission) : base(policy: permission) { }
}
