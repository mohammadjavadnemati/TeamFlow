namespace TeamFlow.Core.DTOs.Dashboard;

public record DashboardDto(
    int TotalProjects,
    int TotalTasks,
    int DoneTasks,
    int InProgressTasks,
    int BlockedTasks,
    int TotalMembers,
    int ActiveSprints,
    IEnumerable<ProjectHealthDto> ProjectHealths,
    IEnumerable<RecentActivityDto> RecentActivities
);

public record ProjectHealthDto(
    Guid ProjectId,
    string ProjectName,
    string Color,
    double HealthScore,
    string HealthLabel,
    string HealthEmoji
);

public record RecentActivityDto(
    string Description,
    string UserFullName,
    string? UserAvatarUrl,
    DateTime CreatedAt
);