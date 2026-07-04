using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.File;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/projects/{projectId:guid}/tasks/{taskId:guid}/files")]
[Authorize]
[Produces("application/json")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    public FilesController(IFileService fileService) => _fileService = fileService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid workspaceId, Guid projectId, Guid taskId)
    {
        var result = await _fileService.GetAllAsync(workspaceId, projectId, taskId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<FileAttachmentDto>>.Ok(result));
    }

    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid workspaceId, Guid projectId, Guid taskId, IFormFile file)
    {
        var result = await _fileService.UploadAsync(workspaceId, projectId, taskId, CurrentUserId, file);
        return Ok(ApiResponse<FileAttachmentDto>.Ok(result, "فایل با موفقیت آپلود شد."));
    }

    [HttpDelete("{fileId:guid}")]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid projectId, Guid taskId, Guid fileId)
    {
        await _fileService.DeleteAsync(workspaceId, projectId, taskId, fileId, CurrentUserId);
        return Ok(ApiResponse.Ok("فایل حذف شد."));
    }
}