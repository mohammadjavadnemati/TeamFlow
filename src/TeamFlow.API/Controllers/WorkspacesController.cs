using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamFlow.Core.Common;
using TeamFlow.Core.DTOs.Workspace;
using TeamFlow.Core.Interfaces;

namespace TeamFlow.API.Controllers;

[ApiController]
[Route("api/v1/workspaces")]
[Authorize]
[Produces("application/json")]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;

    public WorkspacesController(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    private Guid CurrentUserId => Guid.Parse(
        User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>ساخت Workspace جدید</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request)
    {
        var result = await _workspaceService.CreateAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<WorkspaceDto>.Ok(result, "Workspace با موفقیت ساخته شد."));
    }

    /// <summary>لیست Workspace های کاربر</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _workspaceService.GetUserWorkspacesAsync(CurrentUserId);
        return Ok(ApiResponse<IEnumerable<WorkspaceDto>>.Ok(result));
    }

    /// <summary>جزئیات یک Workspace</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _workspaceService.GetByIdAsync(id, CurrentUserId);
        return Ok(ApiResponse<WorkspaceDetailDto>.Ok(result));
    }

    /// <summary>ویرایش Workspace — حداقل Admin</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkspaceRequest request)
    {
        var result = await _workspaceService.UpdateAsync(id, CurrentUserId, request);
        return Ok(ApiResponse<WorkspaceDto>.Ok(result, "Workspace با موفقیت ویرایش شد."));
    }

    /// <summary>حذف Workspace — فقط Owner</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _workspaceService.DeleteAsync(id, CurrentUserId);
        return Ok(ApiResponse.Ok("Workspace با موفقیت حذف شد."));
    }

    // ─── Members ──────────────────────────────────────────────────────────────

    /// <summary>لیست اعضا</summary>
    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var result = await _workspaceService.GetMembersAsync(id, CurrentUserId);
        return Ok(ApiResponse<IEnumerable<WorkspaceMemberDto>>.Ok(result));
    }

    /// <summary>دعوت عضو جدید — حداقل Admin</summary>
    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> InviteMember(Guid id, [FromBody] InviteMemberRequest request)
    {
        var result = await _workspaceService.InviteMemberAsync(id, CurrentUserId, request);
        return Ok(ApiResponse<WorkspaceMemberDto>.Ok(result, "عضو با موفقیت دعوت شد."));
    }

    /// <summary>تغییر Role عضو — حداقل Admin</summary>
    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid id, Guid userId, [FromBody] UpdateMemberRoleRequest request)
    {
        await _workspaceService.UpdateMemberRoleAsync(id, CurrentUserId, userId, request);
        return Ok(ApiResponse.Ok("Role با موفقیت تغییر یافت."));
    }

    /// <summary>حذف عضو — حداقل Admin</summary>
    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        await _workspaceService.RemoveMemberAsync(id, CurrentUserId, userId);
        return Ok(ApiResponse.Ok("عضو با موفقیت حذف شد."));
    }
}