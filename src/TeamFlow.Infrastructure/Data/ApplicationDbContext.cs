using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.Entities;

namespace TeamFlow.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Workspace>(w =>
{
    w.HasKey(x => x.Id);
    w.Property(x => x.Name).HasMaxLength(100).IsRequired();
    w.Property(x => x.Description).HasMaxLength(500);
    w.Property(x => x.LogoUrl).HasMaxLength(500);
});

        builder.Entity<WorkspaceMember>(m =>
        {
            m.HasKey(x => x.Id);
            m.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique();
            m.HasOne(x => x.Workspace)
             .WithMany(x => x.Members)
             .HasForeignKey(x => x.WorkspaceId)
             .OnDelete(DeleteBehavior.Cascade);
            m.HasOne(x => x.User)
             .WithMany()
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        base.OnModelCreating(builder);

        // Rename Identity tables
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.Entity<ApplicationUser>(u =>
        {
            u.Property(x => x.FirstName).HasMaxLength(50).IsRequired();
            u.Property(x => x.LastName).HasMaxLength(50).IsRequired();
            u.Property(x => x.AvatarUrl).HasMaxLength(500);
        });

        builder.Entity<RefreshToken>(rt =>
        {
            rt.HasKey(x => x.Id);
            rt.Property(x => x.Token).HasMaxLength(500).IsRequired();
            rt.HasIndex(x => x.Token).IsUnique();
            rt.HasOne(x => x.User)
              .WithMany(x => x.RefreshTokens)
              .HasForeignKey(x => x.UserId)
              .OnDelete(DeleteBehavior.Cascade);
        });
    }
}