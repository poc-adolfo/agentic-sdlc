namespace AdminApi.Tests;

public class RoleServiceSetPermissionsTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private Role _systemRole = null!;
    private Role _normalRole = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();

        foreach (var id in new[] { "p1", "p2", "p3" })
            _db.Permissions.Add(new Permission { Id = id, Description = id });
        await _db.SaveChangesAsync();

        _systemRole = new Role { Name = "System", Description = "sys", IsSystem = true };
        _normalRole = new Role { Name = "Normal", Description = "norm", IsSystem = false };
        _db.Roles.AddRange(_systemRole, _normalRole);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    private RoleService CreateService()
    {
        var loggerMock = new Mock<ILogger<RoleService>>();
        return new RoleService(_db, loggerMock.Object);
    }

    private List<string> CurrentPerms(Guid roleId)
        => _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId).OrderBy(p => p).ToList();

    private async Task SeedPermissionsAsync(Role role, params string[] permIds)
    {
        foreach (var p in permIds)
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = p });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task NonSystemRole_AddsAndRemovesPermsPerDesired()
    {
        await SeedPermissionsAsync(_normalRole, "p1", "p2");

        var svc = CreateService();
        await svc.SetPermissionsAsync(_normalRole.Id,
            new SetPermissionsDto(new List<string> { "p2", "p3" }), Guid.Empty);

        Assert.Equal(new[] { "p2", "p3" }, CurrentPerms(_normalRole.Id));
    }

    [Fact]
    public async Task SystemRole_DoesNotRemoveMissingPerms_OnlyAddsNew()
    {
        await SeedPermissionsAsync(_systemRole, "p1", "p2");

        var svc = CreateService();
        await svc.SetPermissionsAsync(_systemRole.Id,
            new SetPermissionsDto(new List<string> { "p2", "p3" }), Guid.Empty);

        Assert.Equal(new[] { "p1", "p2", "p3" }, CurrentPerms(_systemRole.Id));
    }

    [Fact]
    public async Task SystemRole_AttemptRemove_DoesNotChangeExistingSet()
    {
        await SeedPermissionsAsync(_systemRole, "p1", "p2");

        var svc = CreateService();
        await svc.SetPermissionsAsync(_systemRole.Id,
            new SetPermissionsDto(new List<string>()), Guid.Empty);

        Assert.Equal(new[] { "p1", "p2" }, CurrentPerms(_systemRole.Id));
    }
}
