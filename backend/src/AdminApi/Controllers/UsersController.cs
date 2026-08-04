using AdminApi.Authorization;
using AdminApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Controllers;

/// <summary>
/// Administração de usuários (seção 5 da spec).
/// Toda escrita exige a permissão granular correspondente — não apenas
/// o papel Administrador.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users) => _users = users;

    private Guid ActingUserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [HasPermission(PermissionsCatalog.UsersList)]
    public async Task<IActionResult> List(
        [FromQuery] string? name, [FromQuery] string? email,
        [FromQuery] string? status, [FromQuery] string? role,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _users.ListAsync(new UserFilter(name, email, status, role), page, pageSize);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionsCatalog.UsersView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var u = await _users.GetAsync(id);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPost]
    [HasPermission(PermissionsCatalog.UsersCreate)]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
    {
        try
        {
            var u = await _users.CreateAsync(dto, ActingUserId);
            return u is null
                ? BadRequest(new { error = "Failed to create user" })
                : CreatedAtAction(nameof(Get), new { id = u.Id }, u);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.UsersEdit)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
    {
        var u = await _users.UpdateAsync(id, dto, ActingUserId);
        return u is null ? NotFound() : Ok(u);
    }

    [HttpPost("{id:guid}/disable")]
    [HasPermission(PermissionsCatalog.UsersDisable)]
    public async Task<IActionResult> Disable(Guid id)
    {
        var ok = await _users.DisableAsync(id, ActingUserId);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("{id:guid}/reactivate")]
    [HasPermission(PermissionsCatalog.UsersDisable)]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        var ok = await _users.ReactivateAsync(id, ActingUserId);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Atribui papéis a um usuário (multi-seleção, seção 5). Exige roles:assign.</summary>
    [HttpPut("{id:guid}/roles")]
    [HasPermission(PermissionsCatalog.RolesAssign)]
    public async Task<IActionResult> AssignRoles(Guid id, [FromBody] AssignRolesDto dto)
    {
        var ok = await _users.AssignRolesAsync(id, dto, ActingUserId);
        return ok ? NoContent() : NotFound();
    }
}
