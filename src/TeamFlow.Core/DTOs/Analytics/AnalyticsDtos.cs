namespace TeamFlow.Core.DTOs.Analytics;

public record TaskCompletionRateDto(
    double Rate,
    int TotalTasks,
    int CompletedTasks
);

public record TasksPerUserDto(
    Guid UserId,
    string UserFullName,
    string? AvatarUrl,
    int TotalTasks,
    int CompletedTasks,
    int InProgressTasks,
    int BlockedTasks,
    bool IsOverloaded
);

public record TasksPerStatusDto(
    string Status,
    int Count,
    double Percentage
);

public record TasksPerPriorityDto(
    string Priority,
    int Count,
    double Percentage
);

public record BurndownDataDto(
    IEnumerable<BurndownPointDto> Ideal,
    IEnumerable<BurndownPointDto> Actual
);

public record BurndownPointDto(
    DateTime Date,
    int RemainingTasks
);

public record SprintProgressDto(
    Guid SprintId,
    string SprintName,
    int TotalTasks,
    int CompletedTasks,
    double ProgressPercentage,
    int DaysTotal,
    int DaysRemaining,
    bool IsAtRisk
);