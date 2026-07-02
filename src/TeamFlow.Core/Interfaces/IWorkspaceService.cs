using TeamFlow.Core.DTOs.Workspace;
using TeamFlow.Core.Enums;

namespace TeamFlow.Core.Interfaces;

public interface IWorkspaceService
{
    Task<WorkspaceDto> CreateAsync(Guid userId, CreateWorkspaceRequest request);
    Task<IEnumerable<WorkspaceDto>> GetUserWorkspacesAsync(Guid userId);
    Task<WorkspaceDetailDto> GetByIdAsync(Guid workspaceId, Guid userId);
    Task<WorkspaceDto> UpdateAsync(Guid workspaceId, Guid userId, UpdateWorkspaceRequest request);
    Task DeleteAsync(Guid workspaceId, Guid userId);

    Task<WorkspaceMemberDto> InviteMemberAsync(Guid workspaceId, Guid inviterId, InviteMemberRequest request);
    Task RemoveMemberAsync(Guid workspaceId, Guid removerId, Guid targetUserId);
    Task UpdateMemberRoleAsync(Guid workspaceId, Guid requesterId, Guid targetUserId, UpdateMemberRoleRequest request);
    Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid userId);

    Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, Guid userId);
}