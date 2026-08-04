using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace AdminApi.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<global::Program>
{
    private readonly SqliteConnection _connection;

    public bool AllowBootstrap { get; set; }

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // UseSetting writes directly to the IWebHostBuilder settings and makes
        // the test signing key available early enough for minimal-hosting
        // configuration (Program.cs) before appsettings.json can supply its
        // intentionally empty production value.
        builder.UseSetting("Jwt:Key", "TestJwtSigningKey_Min32Chars_Enough!!");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ALLOW_ADMIN_BOOTSTRAP"] = AllowBootstrap ? "true" : "false",
                // Keep this override for components that read the application
                // configuration after the host has been built.
                ["Jwt:Key"] = "TestJwtSigningKey_Min32Chars_Enough!!",
            });
        });
        builder.ConfigureTestServices(services =>
        {
            // Substitui o AppDbContext (Npgsql) por SQLite in-memory.
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                            d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            // Cria o schema e marca a migration Init como já aplicada,
            // para que db.Database.Migrate() no Program.cs seja no-op.
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
            db.Database.ExecuteSqlRaw(
                "CREATE TABLE IF NOT EXISTS \"__EFMigrationsHistory\" " +
                "(\"MigrationId\" TEXT NOT NULL PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL)");
            db.Database.ExecuteSqlRaw(
                "INSERT OR IGNORE INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
                "VALUES ('20260804120000_Init', '8.0.8')");
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _connection.Dispose();
        base.Dispose(disposing);
    }
}
