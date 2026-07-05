namespace TeamFlow.Core.DTOs.Report;

public record WeeklyReportDto(
    DateTime From,
    DateTime To,
    int CompletedTasks,
    int BlockedTasks,
    int NewTasks,
    int DelayedTasks,
    double AverageCompletionDays,
    double SprintProgress,
    IEnumerable<UserProductivityDto> UserProductivities
);

public record UserProductivityDto(
    Guid UserId,
    string UserFullName,
    string? AvatarUrl,
    int CompletedTasks,
    int DelayedTasks,
    double AverageCompletionDays,
    double ProductivityScore
);