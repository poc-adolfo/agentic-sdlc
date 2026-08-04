using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AdminApi.Tests;

/// <summary>
/// Testes de integração do endpoint POST /api/users/{id}/reactivate (tarefa 2, issue #16).
///
/// Critérios de aceite cobertos:
/// - Reativar usuário com status=disabled volta para status=active.
/// - Papéis atribuídos ao usuário são preservados antes/depois.
/// - Reativar usuário já active é idempotente (não erro, não muda nada).
/// </summary>
public class UserReactivateIntegrationTests : IAsyncLifetime
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

    /// <summary>
    /// Cria e autentica o usuário admin inicial (primeiro cadastro vira admin
    /// pelo caminho 4.1 do bootstrap). Retorna o HttpClient com bearer token.
    /// </summary>
    private async Task<(HttpClient Client, Guid AdminId)> CreateAuthenticatedAdminClientAsync()
    {
        var adminEmail = $"admin-{Guid.NewGuid():N}@test.com";
        var resp = await _client.PostAsJsonAsync("/api/auth/register",
            new { Name = "Admin", Email = adminEmail, Password = "Password123!" });
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts))!;
        Assert.False(string.IsNullOrEmpty(auth.Token));

        var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.Token);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var admin = await db.Users.SingleAsync(u => u.Email == adminEmail);
        return (adminClient, admin.Id);
    }

    /// <summary>
    /// Cria um usuário alvo (não-admin) via UserManager e atribui papéis a ele.
    /// Retorna o usuário com status e papéis conforme parâmetros.
    /// </summary>
    private async Task<(User User, List<string> RoleNames)> CreateTargetUserWithRolesAsync(
        UserStatus initialStatus)
    {
        var email = $"target-{Guid.NewGuid():N}@test.com";
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User { UserName = email, Email = email, Name = "Target" };
        var result = await users.CreateAsync(user, "Password123!");
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));

        // Cria papéis e atribui ao usuário alvo.
        var role1 = new Role { Name = "Operator", Description = "Op", IsSystem = false };
        var role2 = new Role { Name = "Auditor", Description = "Aud", IsSystem = false };
        db.Roles.AddRange(role1, role2);
        await db.SaveChangesAsync();

        db.UserRolesExplicit.AddRange(
            new UserRole { UserId = user.Id, RoleId = role1.Id, GrantedAt = DateTime.UtcNow, GrantedBy = Guid.Empty },
            new UserRole { UserId = user.Id, RoleId = role2.Id, GrantedAt = DateTime.UtcNow, GrantedBy = Guid.Empty });
        await db.SaveChangesAsync();

        if (initialStatus == UserStatus.Disabled)
        {
            user.Status = UserStatus.Disabled;
            await users.UpdateAsync(user);
        }

        return (user, new List<string> { "Operator", "Auditor" });
    }

    private static async Task<List<string>> GetUserRolesAsync(AppDbContext db, Guid userId)
        => await db.UserRolesExplicit.AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .OrderBy(n => n)
            .ToListAsync();

    private static async Task<UserStatus> GetUserStatusAsync(AppDbContext db, Guid userId)
        => await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Status)
            .SingleAsync();

    [Fact]
    public async Task Reactivate_DisabledUser_ChangesStatusToActive()
    {
        var (adminClient, _) = await CreateAuthenticatedAdminClientAsync();
        var (target, _) = await CreateTargetUserWithRolesAsync(UserStatus.Disabled);

        var resp = await adminClient.PostAsync($"/api/users/{target.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var status = await GetUserStatusAsync(db, target.Id);
        Assert.Equal(UserStatus.Active, status);
    }

    [Fact]
    public async Task Reactivate_DisabledUser_PreservesAssignedRoles()
    {
        var (adminClient, _) = await CreateAuthenticatedAdminClientAsync();
        var (target, expectedRoles) = await CreateTargetUserWithRolesAsync(UserStatus.Disabled);

        // Captura papéis antes.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rolesBefore = await GetUserRolesAsync(db, target.Id);
            Assert.Equal(expectedRoles.OrderBy(n => n), rolesBefore);
        }

        var resp = await adminClient.PostAsync($"/api/users/{target.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        // Papéis depois devem ser idênticos.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var rolesAfter = await GetUserRolesAsync(db, target.Id);
            Assert.Equal(expectedRoles.OrderBy(n => n), rolesAfter);
        }
    }

    [Fact]
    public async Task Reactivate_AlreadyActiveUser_IsIdempotent_NoErrorNoChange()
    {
        var (adminClient, _) = await CreateAuthenticatedAdminClientAsync();
        var (target, expectedRoles) = await CreateTargetUserWithRolesAsync(UserStatus.Active);

        var resp = await adminClient.PostAsync($"/api/users/{target.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Status permanece active.
        var status = await GetUserStatusAsync(db, target.Id);
        Assert.Equal(UserStatus.Active, status);

        // Papéis permanecem idênticos.
        var rolesAfter = await GetUserRolesAsync(db, target.Id);
        Assert.Equal(expectedRoles.OrderBy(n => n), rolesAfter);
    }

    [Fact]
    public async Task Reactivate_NonExistentUser_Returns404()
    {
        var (adminClient, _) = await CreateAuthenticatedAdminClientAsync();
        var fakeId = Guid.NewGuid();

        var resp = await adminClient.PostAsync($"/api/users/{fakeId}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Reactivate_WithoutPermission_Returns403()
    {
        // Cria um usuário SEM a permissão users:disable (sem papel admin).
        var targetEmail = $"noperm-{Guid.NewGuid():N}@test.com";
        var registerResp = await _client.PostAsJsonAsync("/api/auth/register",
            new { Name = "Admin", Email = $"admin-{Guid.NewGuid():N}@test.com", Password = "Password123!" });
        registerResp.EnsureSuccessStatusCode();
        var adminAuth = (await registerResp.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts))!;

        // Agora registra um segundo usuário (não vira admin porque já existe usuário).
        var secondResp = await _client.PostAsJsonAsync("/api/auth/register",
            new { Name = "NoPerm", Email = targetEmail, Password = "Password123!" });
        secondResp.EnsureSuccessStatusCode();
        var secondAuth = (await secondResp.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts))!;

        // Cria um usuário alvo para tentar reativar.
        var (target, _) = await CreateTargetUserWithRolesAsync(UserStatus.Disabled);

        var noPermClient = _factory.CreateClient();
        noPermClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondAuth.Token);

        var resp = await noPermClient.PostAsync($"/api/users/{target.Id}/reactivate", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    private sealed record AuthResponseDto(string Token, bool BootstrappedAdmin, bool FlagActive);
}
