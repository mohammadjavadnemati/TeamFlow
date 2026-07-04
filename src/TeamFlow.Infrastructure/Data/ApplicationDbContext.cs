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
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<ProjectTask> Tasks => Set<ProjectTask>();
    public DbSet<Subtask> Subtasks => Set<Subtask>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<Checklist> Checklists => Set<Checklist>();
    public DbSet<ChecklistItem> ChecklistItems => Set<ChecklistItem>();
    public DbSet<TaskWatcher> TaskWatchers => Set<TaskWatcher>();
    public DbSet<TaskBookmark> TaskBookmarks => Set<TaskBookmark>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

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
        builder.Entity<Project>(p =>
            {
                p.HasKey(x => x.Id);
                p.Property(x => x.Name).HasMaxLength(100).IsRequired();
                p.Property(x => x.Description).HasMaxLength(1000);
                p.Property(x => x.Color).HasMaxLength(7).HasDefaultValue("#6366F1");
                p.HasOne(x => x.Workspace)
                .WithMany()
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
                p.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            });

        builder.Entity<Sprint>(s =>
        {
            s.HasKey(x => x.Id);
            s.Property(x => x.Name).HasMaxLength(100).IsRequired();
            s.Property(x => x.Goal).HasMaxLength(500);
            s.HasOne(x => x.Project)
             .WithMany(x => x.Sprints)
             .HasForeignKey(x => x.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ProjectTask>(t =>
        {
            t.HasKey(x => x.Id);
            t.Property(x => x.Title).HasMaxLength(200).IsRequired();
            t.Property(x => x.Description).HasMaxLength(2000);
            t.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            t.HasOne(x => x.Sprint).WithMany().HasForeignKey(x => x.SprintId).OnDelete(DeleteBehavior.SetNull);
            t.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
            t.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });


        builder.Entity<Subtask>(s =>
        {

            s.HasOne(x => x.Task).WithMany(x => x.Subtasks).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            s.HasKey(x => x.Id);
            s.Property(x => x.Title).HasMaxLength(200).IsRequired();
            s.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<Label>(l =>
        {
            l.HasKey(x => x.Id);
            l.Property(x => x.Name).HasMaxLength(50).IsRequired();
            l.Property(x => x.Color).HasMaxLength(7);
            l.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskLabel>(tl =>
        {
            tl.HasOne(x => x.Task).WithMany(x => x.TaskLabels).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            tl.HasKey(x => new { x.TaskId, x.LabelId });
            tl.HasOne(x => x.Label).WithMany(x => x.TaskLabels).HasForeignKey(x => x.LabelId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<Checklist>(c =>
        {
            c.HasKey(x => x.Id);
            c.Property(x => x.Title).HasMaxLength(100).IsRequired();
            c.HasOne(x => x.Task).WithMany(x => x.Checklists).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ChecklistItem>(i =>
        {
            i.HasKey(x => x.Id);
            i.Property(x => x.Title).HasMaxLength(200).IsRequired();
            i.HasOne(x => x.Checklist).WithMany(x => x.Items).HasForeignKey(x => x.ChecklistId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TaskWatcher>(w =>
        {
            w.HasOne(x => x.Task).WithMany(x => x.Watchers).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            w.HasKey(x => new { x.TaskId, x.UserId });
            w.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        });

        builder.Entity<TaskBookmark>(b =>
        {
            b.HasOne(x => x.Task).WithMany(x => x.Bookmarks).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            b.HasKey(x => new { x.TaskId, x.UserId });
            b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        });
        builder.Entity<Comment>(c =>
{
    c.HasKey(x => x.Id);
    c.Property(x => x.Content).HasMaxLength(2000).IsRequired();
    c.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
    c.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
});

        builder.Entity<FileAttachment>(f =>
        {
            f.HasKey(x => x.Id);
            f.Property(x => x.FileName).HasMaxLength(500).IsRequired();
            f.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            f.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
            f.Property(x => x.BlobUrl).HasMaxLength(1000).IsRequired();
            f.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            f.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ActivityLog>(a =>
        {
            a.HasKey(x => x.Id);
            a.Property(x => x.Description).HasMaxLength(500).IsRequired();
            a.HasOne(x => x.Workspace).WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
            a.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            a.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            a.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Notification>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Title).HasMaxLength(200).IsRequired();
            n.Property(x => x.Message).HasMaxLength(500).IsRequired();
            n.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            n.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.SetNull);
        });

    }
}