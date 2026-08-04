namespace AdminApi.Tests;

public class AdminSeederTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private RoleManager<Role> _roleManager = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();
        _roleManager = BuildRoleManager(_db);
    }

    public Task DisposeAsync()
    {
        _roleManager.Dispose();
        _db.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    private static RoleManager<Role> BuildRoleManager(AppDbContext db)
    {
        var roleStore = new RoleStore<Role, AppDbContext, Guid, UserRole, RoleClaim>(db);
        var validators = new List<IRoleValidator<Role>> { new RoleValidator<Role>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();
        var logger = new LoggerFactory().CreateLogger<RoleManager<Role>>();
        return new RoleManager<Role>(roleStore, validators, normalizer, describer, logger);
    }

    private AdminSeeder CreateSeeder(IConfiguration? cfg = null, ILogger<AdminSeeder>? logger = null)
        => new(_db, _roleManager, cfg ?? new ConfigurationBuilder().Build(),
              logger ?? NullLogger<AdminSeeder>.Instance);

    [Fact]
    public async Task ShouldBootstrapAdmin_UsersEmpty_ReturnsTrue()
    {
        var seeder = CreateSeeder();
        Assert.True(await seeder.ShouldBootstrapAdminAsync());
    }

    [Fact]
    public async Task ShouldBootstrapAdmin_UsersNotEmpty_FlagOff_ReturnsFalse()
    {
        _db.Users.Add(new User { UserName = "a@b.com", Email = "a@b.com", Name = "A" });
        await _db.SaveChangesAsync();

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ALLOW_ADMIN_BOOTSTRAP"] = "false",
            })
            .Build();
        var seeder = CreateSeeder(cfg);
        Assert.False(await seeder.ShouldBootstrapAdminAsync());
    }

    [Fact]
    public async Task ShouldBootstrapAdmin_UsersNotEmpty_FlagOn_ReturnsTrue()
    {
        _db.Users.Add(new User { UserName = "a@b.com", Email = "a@b.com", Name = "A" });
        await _db.SaveChangesAsync();

        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ALLOW_ADMIN_BOOTSTRAP"] = "true",
            })
            .Build();
        var seeder = CreateSeeder(cfg);
        Assert.True(await seeder.ShouldBootstrapAdminAsync());
    }

    [Fact]
    public async Task BootstrapAssignAdmin_IsIdempotent_DoesNotDuplicateUserRole()
    {
        var seeder = CreateSeeder();
        await seeder.SeedAsync();

        var user = new User { UserName = "admin@test.com", Email = "admin@test.com", Name = "Admin" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await seeder.BootstrapAssignAdminAsync(user, viaFlag: false);
        await seeder.BootstrapAssignAdminAsync(user, viaFlag: false);

        var adminRole = await _roleManager.FindByNameAsync(AdminSeeder.SystemAdminRoleName);
        Assert.NotNull(adminRole);
        var userRoles = _db.UserRolesExplicit
            .Where(ur => ur.UserId == user.Id && ur.RoleId == adminRole!.Id).ToList();
        Assert.Single(userRoles);
    }

    [Fact]
    public async Task BootstrapAssignAdmin_ViaFlag_EmitsAuditLog()
    {
        var spy = new SpyLogger<AdminSeeder>();
        var seeder = new AdminSeeder(_db, _roleManager, new ConfigurationBuilder().Build(), spy);
        await seeder.SeedAsync();

        var user = new User { UserName = "flag@test.com", Email = "flag@test.com", Name = "Flag" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await seeder.BootstrapAssignAdminAsync(user, viaFlag: true);

        Assert.Contains(spy.Logs, l =>
            l.Level == LogLevel.Warning &&
            l.Message.Contains("ADMIN BOOTSTRAP") &&
            l.Message.Contains("flag"));
    }
}
