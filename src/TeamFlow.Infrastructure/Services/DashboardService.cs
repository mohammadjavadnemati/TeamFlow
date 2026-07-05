using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Dashboard;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    public DashboardService(ApplicationDbContext context, IWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<DashboardDto> GetAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var projects = await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Include(p => p.Sprints)
            .ToListAsync();

        var projectIds = projects.Select(p => p.Id).ToList();

        var tasks = await _context.Tasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .ToListAsync();

        var members = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspaceId);

        var activeSprints = projects
            .SelectMany(p => p.Sprints)
            .Count(s => s.Status == SprintStatus.Active);

        var recentActivities = await _context.ActivityLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentActivityDto(
                a.Description,
                $"{a.User.FirstName} {a.User.LastName}",
                a.User.AvatarUrl,
                a.CreatedAt))
            .ToListAsync();

        var projectHealths = projects.Select(p =>
        {
            var health = CalculateHealth(p.Sprints.ToList(),
                tasks.Where(t => t.ProjectId == p.Id).ToList(),
                p.EndDate);
            return new ProjectHealthDto(p.Id, p.Name, p.Color, health.Score, health.Label, health.Emoji);
        });

        return new DashboardDto(
            projects.Count,
            tasks.Count,
            tasks.Count(t => t.Status == TaskStatus.Done),
            tasks.Count(t => t.Status == TaskStatus.InProgress),
            tasks.Count(t => t.Status == TaskStatus.Blocked),
            members,
            activeSprints,
            projectHealths,
            recentActivities
        );
    }

    private static (double Score, string Label, string Emoji) CalculateHealth(
        List<Core.Entities.Sprint> sprints,
        List<Core.Entities.ProjectTask> tasks,
        DateTime? endDate)
    {
        double score = 100;

        if (tasks.Count > 0)
        {
            var blockedRatio = (double)tasks.Count(t => t.Status == TaskStatus.Blocked) / tasks.Count;
            score -= blockedRatio * 30;
        }

        if (endDate.HasValue)
        {
            var daysLeft = (endDate.Value - DateTime.UtcNow).TotalDays;
            if (daysLeft < 0) score -= 30;
            else if (daysLeft < 7) score -= 15;
            else if (daysLeft < 14) score -= 5;
        }

        var hasActiveSprint = sprints.Any(s => s.Status == SprintStatus.Active);
        if (!hasActiveSprint) score -= 10;

        score = Math.Max(0, Math.Min(100, score));

        var (label, emoji) = score switch
        {
            >= 80 => ("Excellent", "🟢"),
            >= 60 => ("Good", "🟡"),
            >= 40 => ("Needs Attention", "🟠"),
            _ => ("Critical", "🔴")
        };

        return (Math.Round(score, 1), label, emoji);
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }
}