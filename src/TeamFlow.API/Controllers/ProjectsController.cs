using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Project;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces/{workspaceId:guid}/projects")]
[Authorize]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateProjectRequest request)
    {
        var result = await _projectService.CreateAsync(workspaceId, CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { workspaceId, id = result.Id },
            ApiResponse<ProjectDto>.Ok(result, "پروژه با موفقیت ساخته شد."));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid workspaceId)
    {
        var result = await _projectService.GetAllAsync(workspaceId, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<ProjectDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid workspaceId, Guid id)
    {
        var result = await _projectService.GetByIdAsync(workspaceId, id, CurrentUserId);
        return Ok(ApiResponse<ProjectDetailDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid workspaceId, Guid id, [FromBody] UpdateProjectRequest request)
    {
        var result = await _projectService.UpdateAsync(workspaceId, id, CurrentUserId, request);
        return Ok(ApiResponse<ProjectDto>.Ok(result, "پروژه با موفقیت ویرایش شد."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid id)
    {
        await _projectService.DeleteAsync(workspaceId, id, CurrentUserId);
        return Ok(ApiResponse.Ok("پروژه با موفقیت حذف شد."));
    }
}