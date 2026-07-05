using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Smart;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/smart")]
[Authorize]
[Produces("application/json")]
public class SmartController : ControllerBase
{
    private readonly ISmartService _smartService;
    public SmartController(ISmartService smartService) => _smartService = smartService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>تحلیل بار کاری اعضا</summary>
    [HttpGet("workload")]
    public async Task<IActionResult> GetWorkload(Guid workspaceId)
    {
        var result = await _smartService.GetWorkloadAnalysisAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<WorkloadAnalysisDto>.Ok(result));
    }

    /// <summary>تشخیص ریسک Deadline</summary>
    [HttpGet("projects/{projectId:guid}/sprints/{sprintId:guid}/risk")]
    public async Task<IActionResult> GetDeadlineRisk(Guid workspaceId, Guid projectId, Guid sprintId)
    {
        var result = await _smartService.GetDeadlineRiskAsync(workspaceId, projectId, sprintId, CurrentUserId);
        return Ok(ApiResponse<DeadlineRiskDto>.Ok(result));
    }

    /// <summary>امتیاز بهره‌وری اعضا</summary>
    [HttpGet("productivity")]
    public async Task<IActionResult> GetProductivity(Guid workspaceId)
    {
        var result = await _smartService.GetProductivityScoresAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<ProductivityScoreDto>>.Ok(result));
    }

    /// <summary>آمار تیم</summary>
    [HttpGet("team-statistics")]
    public async Task<IActionResult> GetTeamStatistics(Guid workspaceId)
    {
        var result = await _smartService.GetTeamStatisticsAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<TeamStatisticsDto>.Ok(result));
    }

    /// <summary>Daily Standup خودکار</summary>
    [HttpGet("standup")]
    public async Task<IActionResult> GetDailyStandup(Guid workspaceId)
    {
        var result = await _smartService.GetDailyStandupAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<DailyStandupDto>.Ok(result));
    }
}