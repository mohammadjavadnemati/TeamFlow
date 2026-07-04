using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Task;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/bookmarks")]
[Authorize]
[Produces("application/json")]
public class BookmarksController : ControllerBase
{
    private readonly ITaskService _taskService;
    public BookmarksController(ITaskService taskService) => _taskService = taskService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid workspaceId)
    {
        var result = await _taskService.GetBookmarksAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<TaskDto>>.Ok(result));
    }
}