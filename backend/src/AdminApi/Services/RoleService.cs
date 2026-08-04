using AdminApi.Data;
using AdminApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Services;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RoleService> _log;

    public RoleService(AppDbContext db, ILogger<RoleService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<List<RoleListItem>> ListAsync()
    {
        return await _db.Roles.AsNoTracking()
            .Select(r => new RoleListItem(r.Id, r.Name, r.Description, r.IsSystem))
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async Task<RoleDetail?> GetAsync(Guid id)
    {
        var r = await _db.Roles.AsNoTracking()
            .Include(x => x.RolePermissions)
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return null;
        return new RoleDetail(r.Id, r.Name, r.Description, r.IsSystem,
            r.RolePermissions.Select(rp => rp.PermissionId).ToList(),
            r.UserRoles.Count);
    }

    public async Task<RoleDetail?> CreateAsync(CreateRoleDto dto, Guid actingUserId)
    {
        var role = new Role { Name = dto.Name, Description = dto.Description, IsSystem = false };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        _log.LogInformation("Role {RoleId} created by {Actor}", role.Id, actingUserId);
        return await GetAsync(role.Id);
    }

    public async Task<RoleDetail?> UpdateAsync(Guid id, UpdateRoleDto dto, Guid actingUserId)
    {
        var r = await _db.Roles.FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return null;
        r.Name = dto.Name;
        r.Description = dto.Description;
        await _db.SaveChangesAsync();
        _log.LogInformation("Role {RoleId} updated by {Actor}", id, actingUserId);
        return await GetAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid actingUserId)
    {
        var r = await _db.Roles
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return false;

        // Bloqueado se is_system=true (seção 5).
        if (r.IsSystem)
            throw new InvalidOperationException("System roles cannot be deleted.");

        // Bloqueado se algum usuário ainda tiver esse papel (seção 5).
        if (r.UserRoles.Count > 0)
            throw new InvalidOperationException("Role still assigned to users. Reassign before deletion.");

        _db.Roles.Remove(r);
        await _db.SaveChangesAsync();
        _log.LogInformation("Role {RoleId} deleted by {Actor}", id, actingUserId);
        return true;
    }

    public async Task<bool> SetPermissionsAsync(Guid id, SetPermissionsDto dto, Guid actingUserId)
    {
        var r = await _db.Roles.Include(x => x.RolePermissions)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return false;

        // Papéis is_system ficam em modo somente-leitura / aviso reforçado (seção 5).
        // Aqui sincronizamos as permissões (adiciona e remove conforme a lista
        // desejada) e logamos um aviso quando o papel é is_system. O aviso reforçado
        // é responsabilidade da UI; o backend loga a operação.
        if (r.IsSystem)
            _log.LogWarning("Modifying permissions of SYSTEM role {RoleId} by {Actor}", id, actingUserId);

        var desired = dto.PermissionIds.ToHashSet();

        // Remove permissions não presentes
        foreach (var rp in r.RolePermissions.Where(rp => !desired.Contains(rp.PermissionId)).ToList())
            _db.RolePermissions.Remove(rp);

        // Adiciona permissions novas
        var existing = r.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var perm in desired.Where(p => !existing.Contains(p)))
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = id,
                PermissionId = perm,
            });
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Permissions set for role {RoleId} by {Actor}", id, actingUserId);
        return true;
    }
}
