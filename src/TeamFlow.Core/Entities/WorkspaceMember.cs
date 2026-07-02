using TeamFlow.Core.Enums;

namespace TeamFlow.Core.Entities;

public class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public WorkspaceRole Role { get; set; } = WorkspaceRole.Developer;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}