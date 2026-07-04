using TeamFlow.Core.Enums;

namespace TeamFlow.Core.Entities;

public class ActivityLog : BaseEntity
{
    public ActivityType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; } // JSON

    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? TaskId { get; set; }
    public ProjectTask? Task { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
}