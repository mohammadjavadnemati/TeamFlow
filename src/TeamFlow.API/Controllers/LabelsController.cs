using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Task;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/labels")]
[Authorize]
[Produces("application/json")]
public class LabelsController : ControllerBase
{
    private readonly ITaskService _taskService;
    public LabelsController(ITaskService taskService) => _taskService = taskService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid workspaceId)
    {
        var result = await _taskService.GetLabelsAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<LabelDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateLabelRequest request)
    {
        var result = await _taskService.CreateLabelAsync(workspaceId, CurrentUserId, request);
        return Ok(ApiResponse<LabelDto>.Ok(result, "Label ساخته شد."));
    }

    [HttpDelete("{labelId:guid}")]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid labelId)
    {
        await _taskService.DeleteLabelAsync(workspaceId, labelId, CurrentUserId);
        return Ok(ApiResponse.Ok("Label حذف شد."));
    }
}