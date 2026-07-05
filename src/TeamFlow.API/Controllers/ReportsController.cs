using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Report;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/reports")]
[Authorize]
[Produces("application/json")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    public ReportsController(IReportService reportService) => _reportService = reportService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeekly(Guid workspaceId)
    {
        var result = await _reportService.GetWeeklyReportAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<WeeklyReportDto>.Ok(result));
    }

    [HttpGet("custom")]
    public async Task<IActionResult> GetCustom(Guid workspaceId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var result = await _reportService.GetCustomReportAsync(workspaceId, CurrentUserId, from, to);
        return Ok(ApiResponse<WeeklyReportDto>.Ok(result));
    }
}