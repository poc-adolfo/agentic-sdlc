using AdminApi.Authorization;
using AdminApi.Data;
using AdminApi.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Services;

/// <summary>
/// Seed do catálogo de permissões e do papel Administrador raiz, mais a
/// lógica de bootstrap do admin inicial (seção 4 da spec).
///
/// 4.1 — Primeiro login vira admin: se users estiver vazia, o próximo
///       cadastro/login recebe automaticamente o papel Administrador.
/// 4.2 — Flag ALLOW_ADMIN_BOOTSTRAP: reabre o caminho mesmo com users não vazia
///       (cenário de recuperação). O próximo login bem-sucedido de usuário
///       novo ou sem papel recebe Administrador.
/// </summary>
public class AdminSeeder
{
    public const string SystemAdminRoleName = "Administrador";

    private readonly AppDbContext _db;
    private readonly RoleManager<Role> _roles;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AdminSeeder> _log;

    public AdminSeeder(AppDbContext db, RoleManager<Role> roles,
        IConfiguration cfg, ILogger<AdminSeeder> log)
    {
        _db = db;
        _roles = roles;
        _cfg = cfg;
        _log = log;
    }

    /// <summary>Popula catálogo de permissões e o papel Administrador raiz (is_system=true).</summary>
    public async Task SeedAsync()
    {
        await SeedPermissionsAsync();
        await SeedSystemAdminRoleAsync();
    }

    private async Task SeedPermissionsAsync()
    {
        foreach (var perm in PermissionsCatalog.All)
        {
            if (!await _db.Permissions.AnyAsync(p => p.Id == perm))
            {
                _db.Permissions.Add(new Permission
                {
                    Id = perm,
                    Description = PermissionsCatalog.Descriptions.GetValueOrDefault(perm, ""),
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedSystemAdminRoleAsync()
    {
        var existing = await _roles.FindByNameAsync(SystemAdminRoleName);
        if (existing is null)
        {
            var role = new Role
            {
                Name = SystemAdminRoleName,
                Description = "Administrador raiz (bootstrap) — is_system",
                IsSystem = true,
            };
            var result = await _roles.CreateAsync(role);
            if (!result.Succeeded)
            {
                _log.LogError("Failed to create system admin role: {Errors}",
                    string.Join(";", result.Errors.Select(e => e.Description)));
                return;
            }
            existing = role;
        }

        // Atribui todas as permissões do catálogo ao papel Administrador raiz.
        foreach (var perm in PermissionsCatalog.All)
        {
            if (!await _db.RolePermissions.AnyAsync(rp =>
                    rp.RoleId == existing.Id && rp.PermissionId == perm))
            {
                _db.RolePermissions.Add(new RolePermission
                {
                    RoleId = existing.Id,
                    PermissionId = perm,
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Deve o próximo login receber admin automático?
    /// Caminho 4.1: users vazia. Caminho 4.2: flag ALLOW_ADMIN_BOOTSTRAP ativo.
    /// </summary>
    public async Task<bool> ShouldBootstrapAdminAsync()
    {
        var usersEmpty = !await _db.Users.AnyAsync();
        if (usersEmpty) return true;

        var flag = _cfg["ALLOW_ADMIN_BOOTSTRAP"];
        return string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Atribui o papel Administrador raiz a um usuário (usado no bootstrap 4.1/4.2).
    /// Registra em log de auditoria quem ficou admin, quando, com o flag ativo.
    /// </summary>
    public async Task BootstrapAssignAdminAsync(User user, bool viaFlag)
    {
        var adminRole = await _roles.FindByNameAsync(SystemAdminRoleName);
        if (adminRole is null) return;

        if (!await _db.UserRolesExplicit.AnyAsync(ur =>
                ur.UserId == user.Id && ur.RoleId == adminRole.Id))
        {
            _db.UserRolesExplicit.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = adminRole.Id,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = Guid.Empty, // sistema/bootstrap
            });
            await _db.SaveChangesAsync();

            _log.LogWarning(
                "ADMIN BOOTSTRAP: user {UserId} ({Email}) received Administrator role " +
                "via {Path}. Timestamp {When}",
                user.Id, user.Email,
                viaFlag ? "ALLOW_ADMIN_BOOTSTRAP flag (4.2)" : "empty users table (4.1)",
                DateTime.UtcNow);
        }
    }
}
