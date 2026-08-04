using AdminApi.Authorization;
using AdminApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Controllers;

/// <summary>
/// Expõe o catálogo de permissões (somente leitura — definido em código, seção 3).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PermissionsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var perms = await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.Description })
            .ToListAsync();
        return Ok(perms);
    }
}
