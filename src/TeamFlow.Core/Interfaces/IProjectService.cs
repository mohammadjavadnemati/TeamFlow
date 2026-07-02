using TeamFlow.Core.DTOs.Project;

namespace TeamFlow.Core.Interfaces;

public interface IProjectService
{
    Task<ProjectDto> CreateAsync(Guid workspaceId, Guid userId, CreateProjectRequest request);
    Task<IEnumerable<ProjectDto>> GetAllAsync(Guid workspaceId, Guid userId);
    Task<ProjectDetailDto> GetByIdAsync(Guid workspaceId, Guid projectId, Guid userId);
    Task<ProjectDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid userId, UpdateProjectRequest request);
    Task DeleteAsync(Guid workspaceId, Guid projectId, Guid userId);
}