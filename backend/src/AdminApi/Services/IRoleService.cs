using AdminApi.Data.Entities;

namespace AdminApi.Services;

public interface IRoleService
{
    Task<List<RoleListItem>> ListAsync();
    Task<RoleDetail?> GetAsync(Guid id);
    Task<RoleDetail?> CreateAsync(CreateRoleDto dto, Guid actingUserId);
    Task<RoleDetail?> UpdateAsync(Guid id, UpdateRoleDto dto, Guid actingUserId);
    Task<bool> DeleteAsync(Guid id, Guid actingUserId);
    Task<bool> SetPermissionsAsync(Guid id, SetPermissionsDto dto, Guid actingUserId);
}

public record RoleListItem(Guid Id, string Name, string Description, bool IsSystem);
public record RoleDetail(Guid Id, string Name, string Description, bool IsSystem,
    List<string> Permissions, int UserCount);
public record CreateRoleDto(string Name, string Description);
public record UpdateRoleDto(string Name, string Description);
public record SetPermissionsDto(List<string> PermissionIds);
