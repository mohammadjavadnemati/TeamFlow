using TeamFlow.Core.Enums;

namespace TeamFlow.Core.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public string Color { get; set; } = "#6366F1";

    // FK
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public Guid CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; } = null!;

    // Navigation
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
}