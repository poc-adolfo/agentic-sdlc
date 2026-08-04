using Microsoft.AspNetCore.Authorization;

namespace AdminApi.Authorization;

/// <summary>
/// Requirement de autorização que carrega o nome da permissão exigida
/// (seção 6 da spec). Uma policy é registrada por permissão do catálogo.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}
