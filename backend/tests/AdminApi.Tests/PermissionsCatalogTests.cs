namespace AdminApi.Tests;

public class PermissionsCatalogTests
{
    [Fact]
    public void AllPermissions_HaveCorrespondingDescription()
    {
        foreach (var perm in PermissionsCatalog.All)
        {
            Assert.True(PermissionsCatalog.Descriptions.ContainsKey(perm),
                $"Permission '{perm}' has no description in PermissionsCatalog.Descriptions.");
            Assert.False(string.IsNullOrWhiteSpace(PermissionsCatalog.Descriptions[perm]),
                $"Permission '{perm}' has an empty description.");
        }
    }
}
