using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AdminApi.Tests;

public class AuthControllerIntegrationTests : IAsyncLifetime
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<User> CreateUserAsync(string email, string password, UserStatus status = UserStatus.Active)
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var user = new User { UserName = email, Email = email, Name = email };
        var result = await users.CreateAsync(user, password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
        if (status == UserStatus.Disabled)
        {
            user.Status = UserStatus.Disabled;
            await users.UpdateAsync(user);
        }
        return user;
    }

    [Fact]
    public async Task Login_DisabledUser_Returns403WithoutLockoutIncrement()
    {
        const string email = "disabled@test.com";
        const string password = "Password123!";
        var user = await CreateUserAsync(email, password, UserStatus.Disabled);

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var refreshed = await users.FindByIdAsync(user.Id.ToString());
        Assert.NotNull(refreshed);
        Assert.Equal(0, refreshed!.AccessFailedCount);
        Assert.Null(refreshed.LockoutEnd);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        const string email = "valid@test.com";
        const string password = "Password123!";
        await CreateUserAsync(email, password);

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_ReturnsEffectivePermissionsForAuthenticatedUser()
    {
        const string email = "me@test.com";
        const string password = "Password123!";
        var user = await CreateUserAsync(email, password);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var role = new Role { Name = "Editor", Description = "Editor", IsSystem = false };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
            db.RolePermissions.AddRange(
                new RolePermission { RoleId = role.Id, PermissionId = PermissionsCatalog.UsersEdit },
                new RolePermission { RoleId = role.Id, PermissionId = PermissionsCatalog.UsersView });
            await db.SaveChangesAsync();
            db.UserRolesExplicit.Add(new UserRole
            {
                UserId = user.Id, RoleId = role.Id,
                GrantedAt = DateTime.UtcNow, GrantedBy = Guid.Empty,
            });
            await db.SaveChangesAsync();
        }

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = password });
        loginResp.EnsureSuccessStatusCode();
        var auth = (await loginResp.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts))!;
        Assert.False(string.IsNullOrEmpty(auth.Token));

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var meResp = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);
        var me = (await meResp.Content.ReadFromJsonAsync<MeResponseDto>(JsonOpts))!;
        Assert.Equal(user.Id, me.Id);
        Assert.Contains(PermissionsCatalog.UsersEdit, me.EffectivePermissions);
        Assert.Contains(PermissionsCatalog.UsersView, me.EffectivePermissions);
        Assert.DoesNotContain(PermissionsCatalog.UsersCreate, me.EffectivePermissions);
    }

    [Fact]
    public async Task Register_WithEmptyUsers_AssignsAdministratorRole()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { Name = "First", Email = "first@test.com", Password = "Password123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts))!;
        Assert.True(auth.BootstrappedAdmin);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var createdUser = await db.Users.SingleAsync(u => u.Email == "first@test.com");
        var adminRole = await db.Roles.SingleAsync(r => r.Name == AdminSeeder.SystemAdminRoleName);
        var hasAdmin = await db.UserRolesExplicit
            .AnyAsync(ur => ur.UserId == createdUser.Id && ur.RoleId == adminRole.Id);
        Assert.True(hasAdmin);
    }

    private sealed record AuthResponseDto(string Token, bool BootstrappedAdmin, bool FlagActive);
    private sealed record MeResponseDto(Guid Id, string Name, string Email, string Status,
        List<string> Roles, List<string> EffectivePermissions);
}
