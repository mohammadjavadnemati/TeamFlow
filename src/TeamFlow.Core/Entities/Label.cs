namespace TeamFlow.Core.Entities;

public class Label : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366F1";

    public Guid WorkspaceId { get; set; }
    public Workspace Workspace { get; set; } = null!;

    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}