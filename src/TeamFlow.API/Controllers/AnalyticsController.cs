using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Analytics;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}")]
[Authorize]
[Produces("application/json")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    public AnalyticsController(IAnalyticsService analyticsService) => _analyticsService = analyticsService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("analytics/tasks-per-user")]
    public async Task<IActionResult> GetTasksPerUser(Guid workspaceId)
    {
        var result = await _analyticsService.GetTasksPerUserAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<TasksPerUserDto>>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}/analytics/completion-rate")]
    public async Task<IActionResult> GetCompletionRate(Guid workspaceId, Guid projectId)
    {
        var result = await _analyticsService.GetCompletionRateAsync(workspaceId, projectId, CurrentUserId);
        return Ok(ApiResponse<TaskCompletionRateDto>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}/analytics/tasks-per-status")]
    public async Task<IActionResult> GetTasksPerStatus(Guid workspaceId, Guid projectId)
    {
        var result = await _analyticsService.GetTasksPerStatusAsync(workspaceId, projectId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<TasksPerStatusDto>>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}/analytics/tasks-per-priority")]
    public async Task<IActionResult> GetTasksPerPriority(Guid workspaceId, Guid projectId)
    {
        var result = await _analyticsService.GetTasksPerPriorityAsync(workspaceId, projectId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<TasksPerPriorityDto>>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}/sprints/{sprintId:guid}/analytics/burndown")]
    public async Task<IActionResult> GetBurndown(Guid workspaceId, Guid projectId, Guid sprintId)
    {
        var result = await _analyticsService.GetBurndownAsync(workspaceId, projectId, sprintId, CurrentUserId);
        return Ok(ApiResponse<BurndownDataDto>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}/sprints/{sprintId:guid}/analytics/progress")]
    public async Task<IActionResult> GetSprintProgress(Guid workspaceId, Guid projectId, Guid sprintId)
    {
        var result = await _analyticsService.GetSprintProgressAsync(workspaceId, projectId, sprintId, CurrentUserId);
        return Ok(ApiResponse<SprintProgressDto>.Ok(result));
    }
}