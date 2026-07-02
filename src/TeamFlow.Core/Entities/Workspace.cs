using TeamFlow.Core.Entities;

namespace TeamFlow.Core.Entities;

public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
}