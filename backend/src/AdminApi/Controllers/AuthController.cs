using System.Security.Claims;
using AdminApi.Authorization;
using AdminApi.Data;
using AdminApi.Data.Entities;
using AdminApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Controllers;

/// <summary>
/// Auth/login endpoint. A autenticação em si (mecanismo) é tratada aqui
/// de forma mínima pois a spec cobre autorização, não login. O bootstrap
/// do admin inicial (seção 4) acontece no login/registro.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _users;
    private readonly AppDbContext _db;
    private readonly AdminSeeder _seeder;
    private readonly ITokenService _tokens;
    private readonly SignInManager<User> _signIn;
    private readonly ILogger<AuthController> _log;

    public AuthController(UserManager<User> users, AppDbContext db, AdminSeeder seeder,
        ITokenService tokens, SignInManager<User> signIn, ILogger<AuthController> log)
    {
        _users = users;
        _db = db;
        _seeder = seeder;
        _tokens = tokens;
        _signIn = signIn;
        _log = log;
    }

    public record RegisterDto(string Name, string Email, string Password);
    public record LoginDto(string Email, string Password);
    public record AuthResponse(string Token, bool BootstrappedAdmin, bool FlagActive);
    public record MeResponse(Guid Id, string Name, string Email, string Status,
        List<string> Roles, List<string> EffectivePermissions);

    /// <summary>Registro + login. Se users estiver vazia, vira admin (4.1).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterDto dto)
    {
        var shouldBootstrap = await _seeder.ShouldBootstrapAdminAsync();
        var flagActive = shouldBootstrap && await _db.Users.AnyAsync();

        var user = new User { UserName = dto.Email, Email = dto.Email, Name = dto.Name };
        var result = await _users.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        if (shouldBootstrap)
            await _seeder.BootstrapAssignAdminAsync(user, viaFlag: flagActive);

        var token = await IssueTokenAsync(user);
        return Ok(new AuthResponse(token, shouldBootstrap, flagActive));
    }

    /// <summary>Login. Se ALLOW_ADMIN_BOOTSTRAP ativo e usuário não tem papel, vira admin (4.2).</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null)
            return Unauthorized(new { error = "Invalid credentials" });

        // Conta desativada não pode logar — checar ANTES de validar a senha,
        // para não contabilizar tentativa de login (lockout) numa conta já inativa.
        if (user.Status == UserStatus.Disabled)
            return Forbid();

        var ok = await _signIn.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!ok.Succeeded)
            return Unauthorized(new { error = "Invalid credentials" });

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        // Caminho 4.2: flag ativo + usuário sem nenhum papel → vira admin.
        var shouldBootstrap = await _seeder.ShouldBootstrapAdminAsync();
        var flagActive = shouldBootstrap && await _db.Users.AnyAsync();
        if (shouldBootstrap && flagActive)
        {
            var hasAnyRole = await _db.UserRolesExplicit.AnyAsync(ur => ur.UserId == user.Id);
            if (!hasAnyRole)
                await _seeder.BootstrapAssignAdminAsync(user, viaFlag: true);
        }

        var token = await IssueTokenAsync(user);
        return Ok(new AuthResponse(token, shouldBootstrap && flagActive, flagActive));
    }

    /// <summary>Perfil do usuário autenticado — usado pelo app mobile para obter permissões efetivas.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();

        var u = await _db.Users.AsNoTracking()
            .Include(x => x.UserRoles).ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
            .FirstOrDefaultAsync(x => x.Id == Guid.Parse(userId));
        if (u is null) return NotFound();

        var roles = u.UserRoles.Select(ur => ur.Role.Name).ToList();
        var perms = u.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.PermissionId)
            .Distinct().ToList();

        return Ok(new MeResponse(u.Id, u.Name, u.Email ?? "", u.Status.ToString(), roles, perms));
    }

    private async Task<string> IssueTokenAsync(User user)
    {
        var perms = await EffectivePermissionsResolver.ResolveAsync(_db, user.Id);
        return _tokens.CreateToken(user, perms);
    }
}
