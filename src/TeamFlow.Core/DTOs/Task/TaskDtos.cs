using System.ComponentModel.DataAnnotations;
using TeamFlow.Core.Enums;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Core.DTOs.Task;

// ─── Task ─────────────────────────────────────────────────────────────────────

public record CreateTaskRequest(
    [Required, StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    TaskStatus Status = TaskStatus.Todo,
    TaskPriority Priority = TaskPriority.Medium,
    int? StoryPoints = null,
    DateTime? Deadline = null,
    int? EstimatedTime = null,
    Guid? SprintId = null,
    Guid? AssigneeId = null,
    List<Guid>? LabelIds = null
);

public record UpdateTaskRequest(
    [StringLength(200)] string? Title,
    [StringLength(2000)] string? Description,
    TaskStatus Status = TaskStatus.Todo,
    TaskPriority Priority = TaskPriority.Medium,
    int? StoryPoints = null,
    DateTime? Deadline = null,
    int? EstimatedTime = null,
    int? ActualTime = null,
    Guid? SprintId = null,
    Guid? AssigneeId = null,
    List<Guid>? LabelIds = null
);

public record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    string StatusName,
    TaskPriority Priority,
    string PriorityName,
    int? StoryPoints,
    DateTime? Deadline,
    int? EstimatedTime,
    int? ActualTime,
    Guid ProjectId,
    Guid? SprintId,
    string? SprintName,
    AssigneeDto? Assignee,
    string CreatedByName,
    int SubtaskCount,
    int CompletedSubtaskCount,
    IEnumerable<LabelDto> Labels,
    bool IsWatching,
    bool IsBookmarked,
    DateTime CreatedAt
);

public record TaskDetailDto(
    Guid Id,
    string Title,
    string? Description,
    TaskStatus Status,
    string StatusName,
    TaskPriority Priority,
    string PriorityName,
    int? StoryPoints,
    DateTime? Deadline,
    int? EstimatedTime,
    int? ActualTime,
    Guid ProjectId,
    Guid? SprintId,
    string? SprintName,
    AssigneeDto? Assignee,
    string CreatedByName,
    IEnumerable<SubtaskDto> Subtasks,
    IEnumerable<LabelDto> Labels,
    IEnumerable<ChecklistDto> Checklists,
    IEnumerable<WatcherDto> Watchers,
    bool IsWatching,
    bool IsBookmarked,
    DateTime CreatedAt
);

public record AssigneeDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl
);

// ─── Subtask ───────────────────────────────────────────────────────────────────

public record CreateSubtaskRequest(
    [Required, StringLength(200)] string Title,
    Guid? AssigneeId = null
);

public record UpdateSubtaskRequest(
    [StringLength(200)] string? Title,
    bool? IsCompleted = false,
    Guid? AssigneeId = null
);

public record SubtaskDto(
    Guid Id,
    string Title,
    bool IsCompleted,
    DateTime? CompletedAt,
    AssigneeDto? Assignee
);

// ─── Label ─────────────────────────────────────────────────────────────────────

public record CreateLabelRequest(
    [Required, StringLength(50)] string Name,
    string Color = "#6366F1"
);

public record LabelDto(
    Guid Id,
    string Name,
    string Color
);

// ─── Checklist ─────────────────────────────────────────────────────────────────

public record CreateChecklistRequest(
    [Required, StringLength(100)] string Title
);

public record CreateChecklistItemRequest(
    [Required, StringLength(200)] string Title
);

public record UpdateChecklistItemRequest(
    [StringLength(200)] string? Title,
    bool? IsChecked = false,
    int? Order = 0
);

public record ChecklistDto(
    Guid Id,
    string Title,
    IEnumerable<ChecklistItemDto> Items,
    int TotalItems,
    int CompletedItems
);

public record ChecklistItemDto(
    Guid Id,
    string Title,
    bool IsChecked,
    int Order
);

// ─── Watcher ───────────────────────────────────────────────────────────────────

public record WatcherDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string? AvatarUrl
);

// ─── Filter ────────────────────────────────────────────────────────────────────

public record TaskFilterRequest(
    TaskStatus? Status = null,
    TaskPriority? Priority = null,
    Guid? SprintId = null,
    Guid? AssigneeId = null,
    Guid? LabelId = null,
    DateTime? DeadlineFrom = null,
    DateTime? DeadlineTo = null,
    string? Search = null
);
public record AssignTaskRequest(
    Guid? AssigneeId = null  // null = unassign
);