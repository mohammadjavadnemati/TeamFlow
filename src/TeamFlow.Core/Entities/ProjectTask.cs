using TeamFlow.Core.Enums;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Core.Entities;

public class ProjectTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public int? StoryPoints { get; set; }
    public DateTime? Deadline { get; set; }
    public int? EstimatedTime { get; set; }
    public int? ActualTime { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public Guid CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; } = null!;

    public Guid? AssigneeId { get; set; }
    public ApplicationUser? Assignee { get; set; }

    public ICollection<Subtask> Subtasks { get; set; } = new List<Subtask>();
    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
    public ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();
    public ICollection<TaskWatcher> Watchers { get; set; } = new List<TaskWatcher>();
    public ICollection<TaskBookmark> Bookmarks { get; set; } = new List<TaskBookmark>();
}