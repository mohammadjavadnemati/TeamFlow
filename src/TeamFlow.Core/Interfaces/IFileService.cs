using Microsoft.AspNetCore.Http;
using TeamFlow.Core.DTOs.File;

namespace TeamFlow.Core.Interfaces;

public interface IFileService
{
    Task<FileAttachmentDto> UploadAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, IFormFile file);
    Task<IEnumerable<FileAttachmentDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId);
    Task DeleteAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid fileId, Guid userId);
}