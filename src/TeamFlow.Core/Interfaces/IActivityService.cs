using TeamFlow.Core.DTOs.Activity;
using TeamFlow.Core.Enums;

namespace TeamFlow.Core.Interfaces;

public interface IActivityService
{
    Task LogAsync(Guid workspaceId, Guid userId, ActivityType type, string description, Guid? projectId = null, Guid? taskId = null, string? metadata = null);
    Task<IEnumerable<ActivityLogDto>> GetWorkspaceActivitiesAsync(Guid workspaceId, Guid userId, int page = 1, int pageSize = 20);
    Task<IEnumerable<ActivityLogDto>> GetTaskActivitiesAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
}