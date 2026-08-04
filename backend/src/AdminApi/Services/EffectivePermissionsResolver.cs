using AdminApi.Data;
using AdminApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Services;

/// <summary>
/// Resolve <c>permissões_efetivas(user) = união das permissions de todos os
/// roles em user_roles(user)</c> (seção 3 da spec).
/// </summary>
public static class EffectivePermissionsResolver
{
    public static async Task<HashSet<string>> ResolveAsync(AppDbContext db, Guid userId)
    {
        var perms = await (
            from ur in db.UserRolesExplicit
            join rp in db.RolePermissions on ur.RoleId equals rp.RoleId
            where ur.UserId == userId
            select rp.PermissionId
        ).Distinct().ToListAsync();

        return perms.ToHashSet();
    }
}
