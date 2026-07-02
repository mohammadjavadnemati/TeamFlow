using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Sprint;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/projects/{projectId:guid}/sprints")]
[Authorize]
[Produces("application/json")]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;

    public SprintsController(ISprintService sprintService)
    {
        _sprintService = sprintService;
    }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(Guid workspaceId, Guid projectId, [FromBody] CreateSprintRequest request)
    {
        var result = await _sprintService.CreateAsync(workspaceId, projectId, CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { workspaceId, projectId, id = result.Id },
            ApiResponse<SprintDto>.Ok(result, "Sprint با موفقیت ساخته شد."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid workspaceId, Guid projectId)
    {
        var result = await _sprintService.GetAllAsync(workspaceId, projectId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<SprintDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid workspaceId, Guid projectId, Guid id)
    {
        var result = await _sprintService.GetByIdAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse<SprintDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid workspaceId, Guid projectId, Guid id, [FromBody] UpdateSprintRequest request)
    {
        var result = await _sprintService.UpdateAsync(workspaceId, projectId, id, CurrentUserId, request);
        return Ok(ApiResponse<SprintDto>.Ok(result, "Sprint با موفقیت ویرایش شد."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid projectId, Guid id)
    {
        await _sprintService.DeleteAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse.Ok("Sprint با موفقیت حذف شد."));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid workspaceId, Guid projectId, Guid id)
    {
        var result = await _sprintService.ActivateAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse<SprintDto>.Ok(result, "Sprint فعال شد."));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid workspaceId, Guid projectId, Guid id)
    {
        var result = await _sprintService.CompleteAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse<SprintDto>.Ok(result, "Sprint تکمیل شد."));
    }
}