namespace TeamFlow.Core.Entities;

public class Subtask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }

    public Guid TaskId { get; set; }
    public ProjectTask Task { get; set; } = null!;

    public Guid? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }
}