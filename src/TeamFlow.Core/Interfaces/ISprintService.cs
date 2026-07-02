using TeamFlow.Core.DTOs.Sprint;

namespace TeamFlow.Core.Interfaces;

public interface ISprintService
{
    Task<SprintDto> CreateAsync(Guid workspaceId, Guid projectId, Guid userId, CreateSprintRequest request);
    Task<IEnumerable<SprintDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid userId);
    Task<SprintDto> GetByIdAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId);
    Task<SprintDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId, UpdateSprintRequest request);
    Task DeleteAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId);
    Task<SprintDto> ActivateAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId);
    Task<SprintDto> CompleteAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId);
}