using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AdminApi.Authorization;
using AdminApi.Data;
using AdminApi.Data.Entities;
using AdminApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- EF Core + Postgres ---
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// --- ASP.NET Core Identity ---
builder.Services
    .AddIdentity<User, Role>(o =>
    {
        o.Password.RequiredLength = 8;
        o.Password.RequireNonAlphanumeric = false;
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// --- JWT ---
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "admin-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "admin-mobile";

builder.Services.AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
        };
    });

// --- Authorization com policies dinamicas a partir do catalogo de permissoes ---
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddAuthorization(o =>
    {
        // Registra uma policy por permissao do catalogo.
        foreach (var perm in PermissionsCatalog.All)
        {
            o.AddPolicy(perm, policy => policy.Requirements.Add(new PermissionRequirement(perm)));
        }
    });

// --- Servicos de dominio ---
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<AdminSeeder>();

// --- Swagger (apenas dev) ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

// --- CORS: permissivo em Development, restrito em Production ---
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
{
    if (builder.Environment.IsProduction())
    {
        var origins = builder.Configuration["Cors:AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(origins))
            p.WithOrigins(origins.Split(',', StringSplitOptions.RemoveEmptyEntries))
             .AllowAnyHeader().AllowAnyMethod();
        else
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }
    else
    {
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    }
}));

var app = builder.Build();

// --- Migrate + seed na inicializacao ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var seeder = scope.ServiceProvider.GetRequiredService<AdminSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Expoe a classe 'Program' (gerada implicitamente como internal pelos top-level statements)
// como public partial, permitindo que WebApplicationFactory<Program> a referencie de fora
// do assembly (necessario para testes de integracao em AdminApi.Tests).
public partial class Program { }
