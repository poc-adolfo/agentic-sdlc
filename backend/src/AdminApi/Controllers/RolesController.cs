using AdminApi.Authorization;
using AdminApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Controllers;

/// <summary>
/// Administração de papéis e permissões (seção 5 da spec).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles) => _roles = roles;

    private Guid ActingUserId => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);

    [HttpGet]
    [HasPermission(PermissionsCatalog.UsersList)]
    public async Task<IActionResult> List()
    {
        var list = await _roles.ListAsync();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(PermissionsCatalog.UsersView)]
    public async Task<IActionResult> Get(Guid id)
    {
        var r = await _roles.GetAsync(id);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost]
    [HasPermission(PermissionsCatalog.RolesManage)]
    public async Task<IActionResult> Create([FromBody] CreateRoleDto dto)
    {
        var r = await _roles.CreateAsync(dto, ActingUserId);
        return r is null ? NotFound() : CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(PermissionsCatalog.RolesManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleDto dto)
    {
        var r = await _roles.UpdateAsync(id, dto, ActingUserId);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(PermissionsCatalog.RolesManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var ok = await _roles.DeleteAsync(id, ActingUserId);
            return ok ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>Compor permissões de um papel (matriz/checklist, seção 5). Exige permissions:assign.</summary>
    [HttpPut("{id:guid}/permissions")]
    [HasPermission(PermissionsCatalog.PermissionsAssign)]
    public async Task<IActionResult> SetPermissions(Guid id, [FromBody] SetPermissionsDto dto)
    {
        var ok = await _roles.SetPermissionsAsync(id, dto, ActingUserId);
        return ok ? NoContent() : NotFound();
    }
}
