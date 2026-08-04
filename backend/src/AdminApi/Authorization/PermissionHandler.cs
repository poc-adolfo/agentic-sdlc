using Microsoft.AspNetCore.Authorization;

namespace AdminApi.Authorization;

/// <summary>
/// Handler que lê a claim de permissões efetivas do JWT e decide se a
/// permissão exigida pela policy está presente (seção 6 da spec).
/// A claim <c>perm</c> é preenchida no login com a união das permissões
/// de todos os papéis do usuário.
/// </summary>
public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var perms = context.User.FindAll("perm").Select(c => c.Value).ToHashSet();
        if (perms.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
