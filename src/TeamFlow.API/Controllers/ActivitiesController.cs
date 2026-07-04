using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Activity;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;
    public ActivitiesController(IActivityService activityService) => _activityService = activityService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/activities")]
    public async Task<IActionResult> GetWorkspaceActivities(Guid workspaceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _activityService.GetWorkspaceActivitiesAsync(workspaceId, CurrentUserId, page, pageSize);
        return Ok(ApiResponse<IEnumerable<ActivityLogDto>>.Ok(result));
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/projects/{projectId:guid}/tasks/{taskId:guid}/activities")]
    public async Task<IActionResult> GetTaskActivities(Guid workspaceId, Guid projectId, Guid taskId)
    {
        var result = await _activityService.GetTaskActivitiesAsync(workspaceId, projectId, taskId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<ActivityLogDto>>.Ok(result));
    }
}