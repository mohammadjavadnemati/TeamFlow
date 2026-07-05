using TeamFlow.Core.Enums;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Core.DTOs.Calendar;

public record CalendarEventDto(
    Guid TaskId,
    string Title,
    DateTime Deadline,
    TaskStatus Status,
    string StatusName,
    TaskPriority Priority,
    string PriorityName,
    string? AssigneeName,
    string ProjectName,
    string ProjectColor
);

public record TimelineEventDto(
    string EventType,
    string Description,
    string UserFullName,
    string? UserAvatarUrl,
    DateTime OccurredAt
);