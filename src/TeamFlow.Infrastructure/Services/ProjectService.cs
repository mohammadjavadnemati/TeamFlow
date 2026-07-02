using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Project;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    public ProjectService(ApplicationDbContext context, IWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<ProjectDto> CreateAsync(Guid workspaceId, Guid userId, CreateProjectRequest request)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.ProjectManager);

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            Color = request.Color,
            WorkspaceId = workspaceId,
            CreatedById = userId
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        await _context.Entry(project).Reference(p => p.CreatedBy).LoadAsync();
        return MapToDto(project, 0);
    }

    public async Task<IEnumerable<ProjectDto>> GetAllAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        return await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Include(p => p.CreatedBy)
            .Include(p => p.Sprints)
            .Select(p => MapToDto(p, p.Sprints.Count))
            .ToListAsync();
    }

    public async Task<ProjectDetailDto> GetByIdAsync(Guid workspaceId, Guid projectId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var project = await _context.Projects
            .Include(p => p.CreatedBy)
            .Include(p => p.Sprints)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId)
            ?? throw new KeyNotFoundException("پروژه یافت نشد.");

        var health = CalculateHealthScore(project);

        return new ProjectDetailDto(
            project.Id,
            project.Name,
            project.Description,
            project.StartDate,
            project.EndDate,
            project.Status,
            project.Status.ToString(),
            project.Color,
            project.WorkspaceId,
            $"{project.CreatedBy.FirstName} {project.CreatedBy.LastName}",
            project.Sprints.Select(s => new SprintSummaryDto(
                s.Id, s.Name, s.Status, s.Status.ToString(), s.StartDate, s.EndDate)),
            health.Score,
            health.Label,
            project.CreatedAt
        );
    }

    public async Task<ProjectDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid userId, UpdateProjectRequest request)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.ProjectManager);

        var project = await GetProjectOrThrowAsync(workspaceId, projectId);

        project.Name = request.Name;
        project.Description = request.Description;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;
        project.Color = request.Color;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(project, project.Sprints.Count);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid projectId, Guid userId)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.Admin);

        var project = await GetProjectOrThrowAsync(workspaceId, projectId);
        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
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
            .Include(p => p.CreatedBy)
            .Include(p => p.Sprints)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId)
            ?? throw new KeyNotFoundException("پروژه یافت نشد.");
    }

    private static (double Score, string Label) CalculateHealthScore(Project project)
    {
        double score = 100;

        // اگر پروژه deadline داره و نزدیکه
        if (project.EndDate.HasValue)
        {
            var daysLeft = (project.EndDate.Value - DateTime.UtcNow).TotalDays;
            if (daysLeft < 0) score -= 30;
            else if (daysLeft < 7) score -= 15;
            else if (daysLeft < 14) score -= 5;
        }

        // وضعیت پروژه
        if (project.Status == ProjectStatus.OnHold) score -= 20;
        if (project.Status == ProjectStatus.Cancelled) score -= 50;

        // Sprint فعال نداره
        var hasActiveSprint = project.Sprints.Any(s => s.Status == SprintStatus.Active);
        if (!hasActiveSprint && project.Status == ProjectStatus.Active) score -= 10;

        score = Math.Max(0, Math.Min(100, score));

        var label = score switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Needs Attention",
            _ => "Critical"
        };

        return (Math.Round(score, 1), label);
    }

    private static ProjectDto MapToDto(Project p, int sprintCount)
    {
        var health = CalculateHealthScore(p);
        return new ProjectDto(
            p.Id, p.Name, p.Description, p.StartDate, p.EndDate,
            p.Status, p.Status.ToString(), p.Color, p.WorkspaceId,
            $"{p.CreatedBy.FirstName} {p.CreatedBy.LastName}",
            sprintCount, health.Score, health.Label, p.CreatedAt
        );
    }
}