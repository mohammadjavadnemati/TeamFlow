using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Workspace;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public WorkspaceService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<WorkspaceDto> CreateAsync(Guid userId, CreateWorkspaceRequest request)
    {
        var workspace = new Workspace
        {
            Name = request.Name,
            Description = request.Description,
            LogoUrl = request.LogoUrl
        };

        _context.Workspaces.Add(workspace);

        // سازنده به عنوان Owner اضافه میشه
        _context.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = WorkspaceRole.Owner
        });

        await _context.SaveChangesAsync();
        return MapToDto(workspace, 1);
    }

    public async Task<IEnumerable<WorkspaceDto>> GetUserWorkspacesAsync(Guid userId)
    {
        return await _context.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Workspace)
                .ThenInclude(w => w.Members)
            .Where(m => m.Workspace.IsActive)
            .Select(m => MapToDto(m.Workspace, m.Workspace.Members.Count))
            .ToListAsync();
    }

    public async Task<WorkspaceDetailDto> GetByIdAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var workspace = await _context.Workspaces
            .Include(w => w.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(w => w.Id == workspaceId && w.IsActive)
            ?? throw new KeyNotFoundException("Workspace یافت نشد.");

        return new WorkspaceDetailDto(
            workspace.Id,
            workspace.Name,
            workspace.Description,
            workspace.LogoUrl,
            workspace.CreatedAt,
            workspace.Members.Select(MapMemberToDto)
        );
    }

    public async Task<WorkspaceDto> UpdateAsync(Guid workspaceId, Guid userId, UpdateWorkspaceRequest request)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.Admin);

        var workspace = await GetWorkspaceOrThrowAsync(workspaceId);
        workspace.Name = request.Name;
        workspace.Description = request.Description;
        workspace.LogoUrl = request.LogoUrl;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(workspace, workspace.Members.Count);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid userId)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.Owner);

        var workspace = await GetWorkspaceOrThrowAsync(workspaceId);
        workspace.IsActive = false;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task<WorkspaceMemberDto> InviteMemberAsync(Guid workspaceId, Guid inviterId, InviteMemberRequest request)
    {
        await EnsureRoleAsync(workspaceId, inviterId, WorkspaceRole.Admin);

        var inviterRole = await GetUserRoleAsync(workspaceId, inviterId);

        // نمیتونی کسی با Role بالاتر یا مساوی خودت دعوت کنی
        if ((int)request.Role <= (int)inviterRole!)
            throw new InvalidOperationException("نمیتوانید عضوی با سطح دسترسی بالاتر یا مساوی خود دعوت کنید.");

        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw new KeyNotFoundException("کاربری با این ایمیل یافت نشد.");

        var exists = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == user.Id);

        if (exists)
            throw new InvalidOperationException("این کاربر قبلاً عضو این Workspace است.");

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = user.Id,
            Role = request.Role
        };

        _context.WorkspaceMembers.Add(member);
        await _context.SaveChangesAsync();

        // Load user for response
        member.User = user;
        return MapMemberToDto(member);
    }

    public async Task RemoveMemberAsync(Guid workspaceId, Guid removerId, Guid targetUserId)
    {
        await EnsureRoleAsync(workspaceId, removerId, WorkspaceRole.Admin);

        if (removerId == targetUserId)
            throw new InvalidOperationException("نمیتوانید خودتان را حذف کنید.");

        var removerRole = await GetUserRoleAsync(workspaceId, removerId);
        var targetRole = await GetUserRoleAsync(workspaceId, targetUserId);

        if (targetRole is null)
            throw new KeyNotFoundException("این کاربر عضو Workspace نیست.");

        if ((int)targetRole <= (int)removerRole!)
            throw new InvalidOperationException("نمیتوانید عضوی با سطح دسترسی بالاتر یا مساوی خود را حذف کنید.");

        var member = await _context.WorkspaceMembers
            .FirstAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId);

        _context.WorkspaceMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMemberRoleAsync(Guid workspaceId, Guid requesterId, Guid targetUserId, UpdateMemberRoleRequest request)
    {
        await EnsureRoleAsync(workspaceId, requesterId, WorkspaceRole.Admin);

        if (request.Role == WorkspaceRole.Owner)
            throw new InvalidOperationException("نمیتوان Role را به Owner تغییر داد.");

        var requesterRole = await GetUserRoleAsync(workspaceId, requesterId);

        if ((int)request.Role <= (int)requesterRole!)
            throw new InvalidOperationException("نمیتوانید Role بالاتر یا مساوی خود را اختصاص دهید.");

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == targetUserId)
            ?? throw new KeyNotFoundException("این کاربر عضو Workspace نیست.");

        member.Role = request.Role;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<WorkspaceMemberDto>> GetMembersAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        return await _context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Include(m => m.User)
            .Select(m => MapMemberToDto(m))
            .ToListAsync();
    }

    public async Task<WorkspaceRole?> GetUserRoleAsync(Guid workspaceId, Guid userId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);
        return member?.Role;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var isMember = await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId);

        if (!isMember)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }

    private async Task EnsureRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole minimumRole)
    {
        var role = await GetUserRoleAsync(workspaceId, userId);

        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");

        // عدد کمتر = سطح بالاتر (Owner=1, Viewer=5)
        if ((int)role > (int)minimumRole)
            throw new UnauthorizedAccessException("سطح دسترسی شما برای این عملیات کافی نیست.");
    }

    private async Task<Workspace> GetWorkspaceOrThrowAsync(Guid workspaceId)
    {
        return await _context.Workspaces
            .Include(w => w.Members)
            .FirstOrDefaultAsync(w => w.Id == workspaceId && w.IsActive)
            ?? throw new KeyNotFoundException("Workspace یافت نشد.");
    }

    private static WorkspaceDto MapToDto(Workspace w, int memberCount) => new(
        w.Id, w.Name, w.Description, w.LogoUrl, memberCount, w.CreatedAt
    );

    private static WorkspaceMemberDto MapMemberToDto(WorkspaceMember m) => new(
        m.UserId,
        m.User.FirstName,
        m.User.LastName,
        m.User.Email!,
        m.User.AvatarUrl,
        m.Role,
        m.Role.ToString(),
        m.JoinedAt
    );
}