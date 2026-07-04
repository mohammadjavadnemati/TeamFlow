using TeamFlow.Core.DTOs.Comment;

namespace TeamFlow.Core.Interfaces;

public interface ICommentService
{
    Task<IEnumerable<CommentDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    Task<CommentDto> CreateAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CreateCommentRequest request);
    Task<CommentDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId, UpdateCommentRequest request);
    Task DeleteAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId);
}