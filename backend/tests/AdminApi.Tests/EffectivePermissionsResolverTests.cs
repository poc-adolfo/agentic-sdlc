namespace AdminApi.Tests;

public class EffectivePermissionsResolverTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private User _user = null!;
    private Role _roleA = null!;
    private Role _roleB = null!;
    private Role _roleEmpty = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();

        _db.Permissions.AddRange(
            new Permission { Id = "p1", Description = "P1" },
            new Permission { Id = "p2", Description = "P2" },
            new Permission { Id = "p3", Description = "P3" });
        await _db.SaveChangesAsync();

        _roleA = new Role { Name = "RoleA", Description = "A" };
        _roleB = new Role { Name = "RoleB", Description = "B" };
        _roleEmpty = new Role { Name = "RoleEmpty", Description = "E" };
        _db.Roles.AddRange(_roleA, _roleB, _roleEmpty);
        await _db.SaveChangesAsync();

        _db.RolePermissions.AddRange(
            new RolePermission { RoleId = _roleA.Id, PermissionId = "p1" },
            new RolePermission { RoleId = _roleA.Id, PermissionId = "p2" },
            new RolePermission { RoleId = _roleB.Id, PermissionId = "p2" },
            new RolePermission { RoleId = _roleB.Id, PermissionId = "p3" });
        await _db.SaveChangesAsync();

        _user = new User { UserName = "u@test.com", Email = "u@test.com", Name = "U" };
        _db.Users.Add(_user);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    private UserRole Link(Guid role)
        => new() { UserId = _user.Id, RoleId = role, GrantedAt = DateTime.UtcNow, GrantedBy = Guid.Empty };

    [Fact]
    public async Task UserWithNoRoles_HasEmptyPermissions()
    {
        var perms = await EffectivePermissionsResolver.ResolveAsync(_db, _user.Id);
        Assert.Empty(perms);
    }

    [Fact]
    public async Task UserWithOneRole_HasExactlyThatRolePermissions()
    {
        _db.UserRolesExplicit.Add(Link(_roleA.Id));
        await _db.SaveChangesAsync();

        var perms = await EffectivePermissionsResolver.ResolveAsync(_db, _user.Id);
        Assert.Equal(new[] { "p1", "p2" }, perms.OrderBy(p => p));
    }

    [Fact]
    public async Task UserWithMultipleRoles_HasUnionDeduplicated()
    {
        _db.UserRolesExplicit.Add(Link(_roleA.Id));
        _db.UserRolesExplicit.Add(Link(_roleB.Id));
        await _db.SaveChangesAsync();

        var perms = await EffectivePermissionsResolver.ResolveAsync(_db, _user.Id);
        Assert.Equal(new[] { "p1", "p2", "p3" }, perms.OrderBy(p => p));
    }

    [Fact]
    public async Task RoleWithNoPermissions_ContributesNothing()
    {
        _db.UserRolesExplicit.Add(Link(_roleA.Id));
        _db.UserRolesExplicit.Add(Link(_roleEmpty.Id));
        await _db.SaveChangesAsync();

        var perms = await EffectivePermissionsResolver.ResolveAsync(_db, _user.Id);
        Assert.Equal(new[] { "p1", "p2" }, perms.OrderBy(p => p));
    }
}
