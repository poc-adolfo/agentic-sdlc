using System.Security.Claims;
using AdminApi.Data.Entities;

namespace AdminApi.Services;

public interface ITokenService
{
    /// <summary>Emite um JWT carregando as permissões efetivas do usuário (seção 9).</summary>
    string CreateToken(User user, IEnumerable<string> effectivePermissions);
}
