namespace TeamFlow.Core.DTOs.Smart;

public record WorkloadAnalysisDto(
    IEnumerable<UserWorkloadDto> Users,
    string Summary
);

public record UserWorkloadDto(
    Guid UserId,
    string UserFullName,
    string? AvatarUrl,
    int ActiveTasks,
    int BlockedTasks,
    int OverdueTasks,
    bool IsOverloaded,
    string WorkloadLevel,
    string WorkloadEmoji
);

public record DeadlineRiskDto(
    Guid SprintId,
    string SprintName,
    bool IsAtRisk,
    string RiskLevel,
    string RiskEmoji,
    string Message,
    double CurrentProgress,
    double RequiredProgress,
    int DaysRemaining,
    IEnumerable<RiskyTaskDto> RiskyTasks
);

public record RiskyTaskDto(
    Guid TaskId,
    string Title,
    DateTime Deadline,
    int DaysUntilDeadline,
    string? AssigneeName
);

public record ProductivityScoreDto(
    Guid UserId,
    string UserFullName,
    string? AvatarUrl,
    double Score,
    string ScoreLabel,
    int CompletedTasks,
    double AverageCompletionDays,
    int DelayedTasks,
    string Insight
);

public record TeamStatisticsDto(
    MostActiveUserDto MostActiveMember,
    MostActiveUserDto MostTasksCompleted,
    MostActiveUserDto MostComments,
    MostActiveUserDto MostBugsFixed,
    IEnumerable<ProductivityScoreDto> Leaderboard
);

public record MostActiveUserDto(
    Guid UserId,
    string UserFullName,
    string? AvatarUrl,
    int Count,
    string Label
);

public record DailyStandupDto(
    DateTime Date,
    int CompletedToday,
    int NewBugs,
    int DelayedTasks,
    double SprintProgress,
    string SprintProgressLabel,
    IEnumerable<StandupUserDto> UserUpdates
);

public record StandupUserDto(
    string UserFullName,
    string? AvatarUrl,
    int CompletedToday,
    int InProgress,
    IEnumerable<string> CompletedTaskTitles
);