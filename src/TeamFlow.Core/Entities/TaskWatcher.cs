namespace TeamFlow.Core.Entities;

public class TaskWatcher
{
    public Guid TaskId { get; set; }
    public ProjectTask Task { get; set; } = null!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
}