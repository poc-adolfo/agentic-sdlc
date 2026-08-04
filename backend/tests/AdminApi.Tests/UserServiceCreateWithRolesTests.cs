namespace AdminApi.Tests;

/// <summary>
/// Tarefa 1 (issue #16): POST /api/users com roleIds deve criar o usuário já
/// com os papéis atribuídos, na mesma transação. Sem roleIds, o usuário é
/// criado sem papéis (comportamento anterior preservado).
/// </summary>
public class UserServiceCreateWithRolesTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private AppDbContext _db = null!;
    private Role _roleEditor = null!;
    private Role _roleViewer = null!;
    private Role _roleAdmin = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();
        _db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        await _db.Database.EnsureCreatedAsync();

        _roleEditor = new Role { Name = "Editor", Description = "Editor", IsSystem = false };
        _roleViewer = new Role { Name = "Viewer", Description = "Viewer", IsSystem = false };
        _roleAdmin = new Role { Name = "Administrator", Description = "Admin", IsSystem = true };
        _db.Roles.AddRange(_roleEditor, _roleViewer, _roleAdmin);
        await _db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _db.Dispose();
        _connection.Dispose();
        return Task.CompletedTask;
    }

    private UserService CreateService()
    {
        var loggerMock = new Mock<ILogger<UserService>>();
        var users = BuildUserManager(_db);
        return new UserService(_db, users, loggerMock.Object);
    }

    private static UserManager<User> BuildUserManager(AppDbContext db)
    {
        var userStore = new UserStore<User, Role, AppDbContext, Guid, UserClaim, UserRole,
            IdentityUserLogin<Guid>, IdentityUserToken<Guid>, RoleClaim>(db);
        var validators = new List<IUserValidator<User>> { new UserValidator<User>() };
        var passwordValidators = new List<IPasswordValidator<User>> { new PasswordValidator<User>() };
        var normalizer = new UpperInvariantLookupNormalizer();
        var describer = new IdentityErrorDescriber();
        var hasher = new PasswordHasher<User>();
        return new UserManager<User>(
            userStore,
            Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
            hasher,
            validators,
            passwordValidators,
            normalizer,
            describer,
            null,
            NullLogger<UserManager<User>>.Instance);
    }

    private static readonly Guid Actor = Guid.NewGuid();

    [Fact]
    public async Task Create_WithRoleIds_AssignsRolesInSameTransaction()
    {
        var svc = CreateService();
        var dto = new CreateUserDto(
            "Alice", "alice@test.com", "Password123!",
            new List<Guid> { _roleEditor.Id, _roleViewer.Id });

        var detail = await svc.CreateAsync(dto, Actor);

        Assert.NotNull(detail);
        Assert.Contains("Editor", detail!.Roles);
        Assert.Contains("Viewer", detail.Roles);
        Assert.Equal(2, detail.Roles.Count);

        // Confirma diretamente na tabela user_roles.
        var dbRoles = await _db.UserRolesExplicit
            .Where(ur => ur.UserId == detail.Id)
            .Select(ur => ur.RoleId).ToListAsync();
        Assert.Equal(2, dbRoles.Count);
        Assert.Contains(_roleEditor.Id, dbRoles);
        Assert.Contains(_roleViewer.Id, dbRoles);
    }

    [Fact]
    public async Task Create_WithRoleIds_DeduplicatesRepeatedIds()
    {
        var svc = CreateService();
        var dto = new CreateUserDto(
            "Bob", "bob@test.com", "Password123!",
            new List<Guid> { _roleEditor.Id, _roleEditor.Id });

        var detail = await svc.CreateAsync(dto, Actor);

        Assert.NotNull(detail);
        Assert.Single(detail!.Roles);
        Assert.Contains("Editor", detail.Roles);

        var count = await _db.UserRolesExplicit.CountAsync(ur => ur.UserId == detail.Id);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Create_WithoutRoleIds_CreatesUserWithNoRoles()
    {
        var svc = CreateService();
        var dto = new CreateUserDto("Carol", "carol@test.com", "Password123!", null);

        var detail = await svc.CreateAsync(dto, Actor);

        Assert.NotNull(detail);
        Assert.Empty(detail!.Roles);

        var count = await _db.UserRolesExplicit.CountAsync(ur => ur.UserId == detail.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Create_WithEmptyRoleIds_CreatesUserWithNoRoles()
    {
        var svc = CreateService();
        var dto = new CreateUserDto("Dave", "dave@test.com", "Password123!", new List<Guid>());

        var detail = await svc.CreateAsync(dto, Actor);

        Assert.NotNull(detail);
        Assert.Empty(detail!.Roles);

        var count = await _db.UserRolesExplicit.CountAsync(ur => ur.UserId == detail.Id);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Create_WithInvalidRoleId_ThrowsBeforeUserIsCreated()
    {
        var svc = CreateService();
        var bogus = Guid.NewGuid();
        var dto = new CreateUserDto(
            "Eve", "eve@test.com", "Password123!",
            new List<Guid> { _roleEditor.Id, bogus });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(dto, Actor));
        Assert.Contains(bogus.ToString(), ex.Message);

        // Usuário NÃO deve ter sido persistido.
        var exists = await _db.Users.AnyAsync(u => u.Email == "eve@test.com");
        Assert.False(exists);
    }
}
