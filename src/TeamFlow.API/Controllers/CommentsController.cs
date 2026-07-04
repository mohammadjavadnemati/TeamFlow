using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Comment;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/projects/{projectId:guid}/tasks/{taskId:guid}/comments")]
[Authorize]
[Produces("application/json")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    public CommentsController(ICommentService commentService) => _commentService = commentService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid workspaceId, Guid projectId, Guid taskId)
    {
        var result = await _commentService.GetAllAsync(workspaceId, projectId, taskId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<CommentDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid workspaceId, Guid projectId, Guid taskId, [FromBody] CreateCommentRequest request)
    {
        var result = await _commentService.CreateAsync(workspaceId, projectId, taskId, CurrentUserId, request);
        return Ok(ApiResponse<CommentDto>.Ok(result, "کامنت ثبت شد."));
    }

    [HttpPut("{commentId:guid}")]
    public async Task<IActionResult> Update(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, [FromBody] UpdateCommentRequest request)
    {
        var result = await _commentService.UpdateAsync(workspaceId, projectId, taskId, commentId, CurrentUserId, request);
        return Ok(ApiResponse<CommentDto>.Ok(result, "کامنت ویرایش شد."));
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId)
    {
        await _commentService.DeleteAsync(workspaceId, projectId, taskId, commentId, CurrentUserId);
        return Ok(ApiResponse.Ok("کامنت حذف شد."));
    }
}
