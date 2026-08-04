using AdminApi.Data.Entities;

namespace AdminApi.Services;

public interface IUserService
{
    Task<PagedResult<UserListItem>> ListAsync(UserFilter filter, int page, int pageSize);
    Task<UserDetail?> GetAsync(Guid id);
    Task<UserDetail?> CreateAsync(CreateUserDto dto, Guid actingUserId);
    Task<UserDetail?> UpdateAsync(Guid id, UpdateUserDto dto, Guid actingUserId);
    Task<bool> DisableAsync(Guid id, Guid actingUserId);
    Task<bool> ReactivateAsync(Guid id, Guid actingUserId);
    Task<bool> AssignRolesAsync(Guid id, AssignRolesDto dto, Guid actingUserId);
}

public record UserListItem(Guid Id, string Name, string Email, string Status, List<string> Roles);
public record UserDetail(Guid Id, string Name, string Email, string Status,
    DateTime CreatedAt, DateTime? LastLoginAt, List<string> Roles, List<string> EffectivePermissions);
public record CreateUserDto(string Name, string Email, string Password);
public record UpdateUserDto(string Name, string Email);
public record AssignRolesDto(List<Guid> RoleIds);
public record UserFilter(string? Name, string? Email, string? Status, string? Role);
public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);
