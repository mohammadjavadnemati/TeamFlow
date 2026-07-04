using TeamFlow.Core.DTOs.Task;

namespace TeamFlow.Core.Interfaces;

public interface ITaskService
{
    Task<TaskDto> CreateAsync(Guid workspaceId, Guid projectId, Guid userId, CreateTaskRequest request);
    Task<IEnumerable<TaskDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid userId, TaskFilterRequest filter);
    Task<TaskDetailDto> GetByIdAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    Task<TaskDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, UpdateTaskRequest request);
    Task DeleteAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);

    Task<SubtaskDto> CreateSubtaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CreateSubtaskRequest request);
    Task<SubtaskDto> UpdateSubtaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid subtaskId, Guid userId, UpdateSubtaskRequest request);
    Task DeleteSubtaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid subtaskId, Guid userId);

    Task<LabelDto> CreateLabelAsync(Guid workspaceId, Guid userId, CreateLabelRequest request);
    Task<IEnumerable<LabelDto>> GetLabelsAsync(Guid workspaceId, Guid userId);
    Task DeleteLabelAsync(Guid workspaceId, Guid labelId, Guid userId);

    Task<ChecklistDto> CreateChecklistAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CreateChecklistRequest request);
    Task DeleteChecklistAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid userId);
    Task<ChecklistItemDto> AddChecklistItemAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid userId, CreateChecklistItemRequest request);
    Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid itemId, Guid userId, UpdateChecklistItemRequest request);
    Task DeleteChecklistItemAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid itemId, Guid userId);

    Task WatchAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    Task UnwatchAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    Task BookmarkAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    Task UnbookmarkAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    Task<IEnumerable<TaskDto>> GetBookmarksAsync(Guid workspaceId, Guid userId);
}