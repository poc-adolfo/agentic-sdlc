using AdminApi.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AdminApi.Data;

/// <summary>
/// DbContext que combina as tabelas de Identity (Users, Roles) com as
/// tabelas de junção explícitas role_permissions e user_roles, conforme
/// a seção 3 da spec. As junções são entidades explícitas (não many-to-many
/// implícito do EF) para permitir metadados futuros (granted_at, granted_by).
/// </summary>
public class AppDbContext : IdentityDbContext<User, Role, Guid,
    UserClaim, UserRole, IdentityUserLogin<Guid>, RoleClaim, IdentityUserToken<Guid>>
{
    public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) { }

    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRolesExplicit => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Status).HasConversion<string>().HasMaxLength(16);
        });

        b.Entity<Role>(e =>
        {
            e.ToTable("roles");
            e.Property(r => r.Name).HasMaxLength(64);
            e.Property(r => r.Description).HasMaxLength(256);
        });

        b.Entity<Permission>(e =>
        {
            e.ToTable("permissions");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasMaxLength(64);
            e.HasIndex(p => p.Id).IsUnique();
            e.Property(p => p.Description).HasMaxLength(256);
        });

        b.Entity<RolePermission>(e =>
        {
            e.ToTable("role_permissions");
            e.HasKey(rp => new { rp.RoleId, rp.PermissionId });
            e.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<UserRole>(e =>
        {
            e.ToTable("user_roles");
            e.HasKey(ur => new { ur.UserId, ur.RoleId });
            e.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(ur => ur.GrantedAt).HasDefaultValueSql("now()");
        });

        // Renomeia as tabelas do Identity herdadas para snake_case
        b.Entity<UserClaim>().ToTable("user_claims");
        b.Entity<RoleClaim>().ToTable("role_claims");
        b.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        b.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
    }
}
