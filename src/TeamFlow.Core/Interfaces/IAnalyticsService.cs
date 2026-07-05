using TeamFlow.Core.DTOs.Analytics;

namespace TeamFlow.Core.Interfaces;

public interface IAnalyticsService
{
    Task<TaskCompletionRateDto> GetCompletionRateAsync(Guid workspaceId, Guid projectId, Guid userId);
    Task<IEnumerable<TasksPerUserDto>> GetTasksPerUserAsync(Guid workspaceId, Guid userId);
    Task<IEnumerable<TasksPerStatusDto>> GetTasksPerStatusAsync(Guid workspaceId, Guid projectId, Guid userId);
    Task<IEnumerable<TasksPerPriorityDto>> GetTasksPerPriorityAsync(Guid workspaceId, Guid projectId, Guid userId);
    Task<BurndownDataDto> GetBurndownAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId);
    Task<SprintProgressDto> GetSprintProgressAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId);
}