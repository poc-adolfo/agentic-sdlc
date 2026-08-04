using AdminApi.Data;
using AdminApi.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly UserManager<User> _users;
    private readonly ILogger<UserService> _log;

    public UserService(AppDbContext db, UserManager<User> users, ILogger<UserService> log)
    {
        _db = db;
        _users = users;
        _log = log;
    }

    public async Task<PagedResult<UserListItem>> ListAsync(UserFilter filter, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Name))
            q = q.Where(u => u.Name.Contains(filter.Name));
        if (!string.IsNullOrWhiteSpace(filter.Email))
            q = q.Where(u => (u.Email ?? "").Contains(filter.Email));
        if (!string.IsNullOrWhiteSpace(filter.Status) &&
            Enum.TryParse<UserStatus>(filter.Status, ignoreCase: true, out var st))
            q = q.Where(u => u.Status == st);
        if (!string.IsNullOrWhiteSpace(filter.Role))
            q = q.Where(u => u.UserRoles.Any(ur => ur.Role.Name == filter.Role));

        var total = await q.CountAsync();
        var users = await q.OrderBy(u => u.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new UserListItem(
                u.Id, u.Name, u.Email ?? "", u.Status.ToString(),
                u.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .ToListAsync();

        return new PagedResult<UserListItem>(users, total, page, pageSize);
    }

    public async Task<UserDetail?> GetAsync(Guid id)
    {
        var u = await _db.Users.AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return null;

        var roles = u.UserRoles.Select(ur => ur.Role.Name).ToList();
        var perms = u.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.PermissionId)
            .Distinct().ToList();

        return new UserDetail(u.Id, u.Name, u.Email ?? "", u.Status.ToString(),
            u.CreatedAt, u.LastLoginAt, roles, perms);
    }

    public async Task<UserDetail?> CreateAsync(CreateUserDto dto, Guid actingUserId)
    {
        // Validate role IDs before opening the write transaction so invalid input
        // cannot create a user or leave any partial state behind.
        List<Guid> roleIds = dto.RoleIds is null || dto.RoleIds.Count == 0
            ? new()
            : await ResolveAndValidateRoleIdsAsync(dto.RoleIds);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var user = new User { UserName = dto.Email, Email = dto.Email, Name = dto.Name };
            var result = await _users.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            if (roleIds.Count > 0)
            {
                var now = DateTime.UtcNow;
                foreach (var roleId in roleIds)
                {
                    _db.UserRolesExplicit.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId,
                        GrantedAt = now,
                        GrantedBy = actingUserId,
                    });
                }
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            _log.LogInformation("User {UserId} created with {Count} role(s) by {Actor}",
                user.Id, roleIds.Count, actingUserId);
            return await GetAsync(user.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Garante que todos os roleIds fornecidos existem e retorna a lista
    /// deduplicada. Lança <see cref="InvalidOperationException"/> se algum
    /// papel não for encontrado — o controller converte isso em 400.
    /// </summary>
    private async Task<List<Guid>> ResolveAndValidateRoleIdsAsync(List<Guid> roleIds)
    {
        var distinct = roleIds.Distinct().ToList();
        var found = await _db.Roles
            .Where(r => distinct.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync();
        if (found.Count != distinct.Count)
        {
            var missing = distinct.Except(found).ToList();
            throw new InvalidOperationException(
                $"Role(s) not found: {string.Join(", ", missing.Select(g => g.ToString()))}");
        }
        return distinct;
    }

    public async Task<UserDetail?> UpdateAsync(Guid id, UpdateUserDto dto, Guid actingUserId)
    {
        var u = await _users.FindByIdAsync(id.ToString());
        if (u is null) return null;
        u.Name = dto.Name;
        u.Email = dto.Email;
        u.UserName = dto.Email;
        await _users.UpdateAsync(u);
        return await GetAsync(id);
    }

    public async Task<bool> DisableAsync(Guid id, Guid actingUserId)
    {
        var u = await _users.FindByIdAsync(id.ToString());
        if (u is null) return false;
        u.Status = UserStatus.Disabled;
        await _users.UpdateAsync(u);
        _log.LogInformation("User {UserId} disabled by {Actor}", id, actingUserId);
        return true;
    }

    public async Task<bool> ReactivateAsync(Guid id, Guid actingUserId)
    {
        var u = await _users.FindByIdAsync(id.ToString());
        if (u is null) return false;
        u.Status = UserStatus.Active;
        await _users.UpdateAsync(u);
        _log.LogInformation("User {UserId} reactivated by {Actor}", id, actingUserId);
        return true;
    }

    public async Task<bool> AssignRolesAsync(Guid id, AssignRolesDto dto, Guid actingUserId)
    {
        var u = await _db.Users.Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (u is null) return false;

        var current = u.UserRoles.ToList();
        var desired = dto.RoleIds.ToHashSet();

        // Remove roles não presentes
        foreach (var ur in current.Where(ur => !desired.Contains(ur.RoleId)).ToList())
            _db.UserRolesExplicit.Remove(ur);

        // Adiciona roles novas
        var existing = current.Select(ur => ur.RoleId).ToHashSet();
        foreach (var roleId in desired.Where(r => !existing.Contains(r)))
        {
            _db.UserRolesExplicit.Add(new UserRole
            {
                UserId = id,
                RoleId = roleId,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = actingUserId,
            });
        }

        await _db.SaveChangesAsync();
        _log.LogInformation("Roles reassigned for user {UserId} by {Actor}", id, actingUserId);
        return true;
    }
}
