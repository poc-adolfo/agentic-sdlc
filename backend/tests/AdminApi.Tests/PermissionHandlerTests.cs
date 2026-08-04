using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AdminApi.Tests;

public class PermissionHandlerTests
{
    private static async Task<bool> Succeeds(string required, params string[] permClaims)
    {
        var handler = new PermissionHandler();
        var claims = permClaims.Select(p => new Claim("perm", p)).ToList();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var requirement = new PermissionRequirement(required);
        var context = new AuthorizationHandlerContext(
            new IAuthorizationRequirement[] { requirement }, principal, resource: null);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task ClaimPresent_Succeeds()
        => Assert.True(await Succeeds("users:create", "users:create"));

    [Fact]
    public async Task ClaimAbsent_DoesNotSucceed()
        => Assert.False(await Succeeds("users:create", "users:edit"));

    [Fact]
    public async Task MultiplePermClaims_DedupCorrect()
        => Assert.True(await Succeeds("users:edit", "users:create", "users:edit", "users:edit"));

    [Fact]
    public async Task NoPermClaims_DeniesAll()
    {
        var handler = new PermissionHandler();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim("other", "val") }, "Test"));
        var requirement = new PermissionRequirement("users:create");
        var context = new AuthorizationHandlerContext(
            new IAuthorizationRequirement[] { requirement }, principal, resource: null);
        await handler.HandleAsync(context);
        Assert.False(context.HasSucceeded);
    }
}
