using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Task;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/projects/{projectId:guid}/tasks")]
[Authorize]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    public TasksController(ITaskService taskService) => _taskService = taskService;

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(Guid workspaceId, Guid projectId, [FromBody] CreateTaskRequest request)
    {
        var result = await _taskService.CreateAsync(workspaceId, projectId, CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { workspaceId, projectId, id = result.Id },
            ApiResponse<TaskDto>.Ok(result, "Task با موفقیت ساخته شد."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid workspaceId, Guid projectId, [FromQuery] TaskFilterRequest filter)
    {
        var result = await _taskService.GetAllAsync(workspaceId, projectId, CurrentUserId, filter);
        return Ok(ApiResponse<IEnumerable<TaskDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid workspaceId, Guid projectId, Guid id)
    {
        var result = await _taskService.GetByIdAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse<TaskDetailDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid workspaceId, Guid projectId, Guid id, [FromBody] UpdateTaskRequest request)
    {
        var result = await _taskService.UpdateAsync(workspaceId, projectId, id, CurrentUserId, request);
        return Ok(ApiResponse<TaskDto>.Ok(result, "Task با موفقیت ویرایش شد."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid projectId, Guid id)
    {
        await _taskService.DeleteAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse.Ok("Task با موفقیت حذف شد."));
    }

    // ─── Subtasks ──────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/subtasks")]
    public async Task<IActionResult> CreateSubtask(Guid workspaceId, Guid projectId, Guid id, [FromBody] CreateSubtaskRequest request)
    {
        var result = await _taskService.CreateSubtaskAsync(workspaceId, projectId, id, CurrentUserId, request);
        return Ok(ApiResponse<SubtaskDto>.Ok(result));
    }

    [HttpPut("{id:guid}/subtasks/{subtaskId:guid}")]
    public async Task<IActionResult> UpdateSubtask(Guid workspaceId, Guid projectId, Guid id, Guid subtaskId, [FromBody] UpdateSubtaskRequest request)
    {
        var result = await _taskService.UpdateSubtaskAsync(workspaceId, projectId, id, subtaskId, CurrentUserId, request);
        return Ok(ApiResponse<SubtaskDto>.Ok(result));
    }

    [HttpDelete("{id:guid}/subtasks/{subtaskId:guid}")]
    public async Task<IActionResult> DeleteSubtask(Guid workspaceId, Guid projectId, Guid id, Guid subtaskId)
    {
        await _taskService.DeleteSubtaskAsync(workspaceId, projectId, id, subtaskId, CurrentUserId);
        return Ok(ApiResponse.Ok("Subtask حذف شد."));
    }

    // ─── Checklists ────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/checklists")]
    public async Task<IActionResult> CreateChecklist(Guid workspaceId, Guid projectId, Guid id, [FromBody] CreateChecklistRequest request)
    {
        var result = await _taskService.CreateChecklistAsync(workspaceId, projectId, id, CurrentUserId, request);
        return Ok(ApiResponse<ChecklistDto>.Ok(result));
    }

    [HttpDelete("{id:guid}/checklists/{checklistId:guid}")]
    public async Task<IActionResult> DeleteChecklist(Guid workspaceId, Guid projectId, Guid id, Guid checklistId)
    {
        await _taskService.DeleteChecklistAsync(workspaceId, projectId, id, checklistId, CurrentUserId);
        return Ok(ApiResponse.Ok("Checklist حذف شد."));
    }

    [HttpPost("{id:guid}/checklists/{checklistId:guid}/items")]
    public async Task<IActionResult> AddChecklistItem(Guid workspaceId, Guid projectId, Guid id, Guid checklistId, [FromBody] CreateChecklistItemRequest request)
    {
        var result = await _taskService.AddChecklistItemAsync(workspaceId, projectId, id, checklistId, CurrentUserId, request);
        return Ok(ApiResponse<ChecklistItemDto>.Ok(result));
    }

    [HttpPut("{id:guid}/checklists/{checklistId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> UpdateChecklistItem(Guid workspaceId, Guid projectId, Guid id, Guid checklistId, Guid itemId, [FromBody] UpdateChecklistItemRequest request)
    {
        var result = await _taskService.UpdateChecklistItemAsync(workspaceId, projectId, id, checklistId, itemId, CurrentUserId, request);
        return Ok(ApiResponse<ChecklistItemDto>.Ok(result));
    }

    [HttpDelete("{id:guid}/checklists/{checklistId:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> DeleteChecklistItem(Guid workspaceId, Guid projectId, Guid id, Guid checklistId, Guid itemId)
    {
        await _taskService.DeleteChecklistItemAsync(workspaceId, projectId, id, checklistId, itemId, CurrentUserId);
        return Ok(ApiResponse.Ok("آیتم حذف شد."));
    }

    // ─── Watch & Bookmark ──────────────────────────────────────────────────────

    [HttpPost("{id:guid}/watch")]
    public async Task<IActionResult> Watch(Guid workspaceId, Guid projectId, Guid id)
    {
        await _taskService.WatchAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse.Ok("Task را دنبال می‌کنید."));
    }

    [HttpDelete("{id:guid}/watch")]
    public async Task<IActionResult> Unwatch(Guid workspaceId, Guid projectId, Guid id)
    {
        await _taskService.UnwatchAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse.Ok("دنبال کردن Task لغو شد."));
    }

    [HttpPost("{id:guid}/bookmark")]
    public async Task<IActionResult> Bookmark(Guid workspaceId, Guid projectId, Guid id)
    {
        await _taskService.BookmarkAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse.Ok("Task ذخیره شد."));
    }

    [HttpDelete("{id:guid}/bookmark")]
    public async Task<IActionResult> Unbookmark(Guid workspaceId, Guid projectId, Guid id)
    {
        await _taskService.UnbookmarkAsync(workspaceId, projectId, id, CurrentUserId);
        return Ok(ApiResponse.Ok("Task از ذخیره‌شده‌ها حذف شد."));
    }
}