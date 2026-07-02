using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Sprint;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class SprintService : ISprintService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    public SprintService(ApplicationDbContext context, IWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<SprintDto> CreateAsync(Guid workspaceId, Guid projectId, Guid userId, CreateSprintRequest request)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.ProjectManager);
        var project = await GetProjectOrThrowAsync(workspaceId, projectId);

        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("تاریخ پایان باید بعد از تاریخ شروع باشد.");

        var sprint = new Sprint
        {
            Name = request.Name,
            Goal = request.Goal,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            ProjectId = projectId
        };

        _context.Sprints.Add(sprint);
        await _context.SaveChangesAsync();

        return MapToDto(sprint, project.Name);
    }

    public async Task<IEnumerable<SprintDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);
        var project = await GetProjectOrThrowAsync(workspaceId, projectId);

        return await _context.Sprints
            .Where(s => s.ProjectId == projectId)
            .Select(s => MapToDto(s, project.Name))
            .ToListAsync();
    }

    public async Task<SprintDto> GetByIdAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);
        var project = await GetProjectOrThrowAsync(workspaceId, projectId);
        var sprint = await GetSprintOrThrowAsync(projectId, sprintId);
        return MapToDto(sprint, project.Name);
    }

    public async Task<SprintDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId, UpdateSprintRequest request)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.ProjectManager);
        var project = await GetProjectOrThrowAsync(workspaceId, projectId);
        var sprint = await GetSprintOrThrowAsync(projectId, sprintId);

        if (sprint.Status == SprintStatus.Completed)
            throw new InvalidOperationException("Sprint تکمیل‌شده قابل ویرایش نیست.");

        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("تاریخ پایان باید بعد از تاریخ شروع باشد.");

        sprint.Name = request.Name;
        sprint.Goal = request.Goal;
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;
        sprint.Status = request.Status;
        sprint.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(sprint, project.Name);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.ProjectManager);
        await GetProjectOrThrowAsync(workspaceId, projectId);
        var sprint = await GetSprintOrThrowAsync(projectId, sprintId);

        if (sprint.Status == SprintStatus.Active)
            throw new InvalidOperationException("Sprint فعال را نمیتوان حذف کرد.");

        _context.Sprints.Remove(sprint);
        await _context.SaveChangesAsync();
    }

    public async Task<SprintDto> ActivateAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.ProjectManager);
        var project = await GetProjectOrThrowAsync(workspaceId, projectId);
        var sprint = await GetSprintOrThrowAsync(projectId, sprintId);

        var hasActive = await _context.Sprints
            .AnyAsync(s => s.ProjectId == projectId && s.Status == SprintStatus.Active);

        if (hasActive)
            throw new InvalidOperationException("این پروژه قبلاً یک Sprint فعال دارد.");

        sprint.Status = SprintStatus.Active;
        sprint.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(sprint, project.Name);
    }

    public async Task<SprintDto> CompleteAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.ProjectManager);
        var project = await GetProjectOrThrowAsync(workspaceId, projectId);
        var sprint = await GetSprintOrThrowAsync(projectId, sprintId);

        if (sprint.Status != SprintStatus.Active)
            throw new InvalidOperationException("فقط Sprint فعال را میتوان تکمیل کرد.");

        sprint.Status = SprintStatus.Completed;
        sprint.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(sprint, project.Name);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }

    private async Task EnsureRoleAsync(Guid workspaceId, Guid userId, WorkspaceRole minimumRole)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
        if ((int)role > (int)minimumRole)
            throw new UnauthorizedAccessException("سطح دسترسی شما برای این عملیات کافی نیست.");
    }

    private async Task<Project> GetProjectOrThrowAsync(Guid workspaceId, Guid projectId)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId)
            ?? throw new KeyNotFoundException("پروژه یافت نشد.");
    }

    private async Task<Sprint> GetSprintOrThrowAsync(Guid projectId, Guid sprintId)
    {
        return await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Sprint یافت نشد.");
    }

    private static SprintDto MapToDto(Sprint s, string projectName) => new(
        s.Id, s.Name, s.Goal, s.StartDate, s.EndDate,
        s.Status, s.Status.ToString(), s.ProjectId, projectName,
        Math.Max(0, (int)(s.EndDate - DateTime.UtcNow).TotalDays),
        s.CreatedAt
    );
}