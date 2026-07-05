using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Calendar;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}")]
[Authorize]
[Produces("application/json")]
public class CalendarController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    public CalendarController(ICalendarService calendarService) => _calendarService = calendarService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("calendar")]
    public async Task<IActionResult> GetDeadlines(
        Guid workspaceId,
        [FromQuery] int year = 0,
        [FromQuery] int month = 0)
    {
        if (year == 0) year = DateTime.UtcNow.Year;
        if (month == 0) month = DateTime.UtcNow.Month;

        var result = await _calendarService.GetDeadlinesAsync(workspaceId, CurrentUserId, year, month);
        return Ok(ApiResponse<IEnumerable<CalendarEventDto>>.Ok(result));
    }

    [HttpGet("projects/{projectId:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid workspaceId, Guid projectId)
    {
        var result = await _calendarService.GetProjectTimelineAsync(workspaceId, projectId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<TimelineEventDto>>.Ok(result));
    }
}