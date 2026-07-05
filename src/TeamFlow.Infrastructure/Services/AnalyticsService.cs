using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Analytics;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    private const int OverloadThreshold = 10;

    public AnalyticsService(ApplicationDbContext context, IWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<TaskCompletionRateDto> GetCompletionRateAsync(Guid workspaceId, Guid projectId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == TaskStatus.Done);
        var rate = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1);

        return new TaskCompletionRateDto(rate, total, completed);
    }

    public async Task<IEnumerable<TasksPerUserDto>> GetTasksPerUserAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var members = await _context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Include(m => m.User)
            .ToListAsync();

        var projectIds = await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync();

        var tasks = await _context.Tasks
            .Where(t => projectIds.Contains(t.ProjectId) && t.AssigneeId != null)
            .ToListAsync();

        return members.Select(m =>
        {
            var userTasks = tasks.Where(t => t.AssigneeId == m.UserId).ToList();
            return new TasksPerUserDto(
                m.UserId,
                $"{m.User.FirstName} {m.User.LastName}",
                m.User.AvatarUrl,
                userTasks.Count,
                userTasks.Count(t => t.Status == TaskStatus.Done),
                userTasks.Count(t => t.Status == TaskStatus.InProgress),
                userTasks.Count(t => t.Status == TaskStatus.Blocked),
                userTasks.Count(t => t.Status != TaskStatus.Done) >= OverloadThreshold
            );
        });
    }

    public async Task<IEnumerable<TasksPerStatusDto>> GetTasksPerStatusAsync(Guid workspaceId, Guid projectId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var total = tasks.Count;

        return Enum.GetValues<TaskStatus>().Select(status =>
        {
            var count = tasks.Count(t => t.Status == status);
            return new TasksPerStatusDto(
                status.ToString(),
                count,
                total == 0 ? 0 : Math.Round((double)count / total * 100, 1)
            );
        });
    }

    public async Task<IEnumerable<TasksPerPriorityDto>> GetTasksPerPriorityAsync(Guid workspaceId, Guid projectId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        var total = tasks.Count;

        return Enum.GetValues<TaskPriority>().Select(priority =>
        {
            var count = tasks.Count(t => t.Priority == priority);
            return new TasksPerPriorityDto(
                priority.ToString(),
                count,
                total == 0 ? 0 : Math.Round((double)count / total * 100, 1)
            );
        });
    }

    public async Task<BurndownDataDto> GetBurndownAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Sprint یافت نشد.");

        var tasks = await _context.Tasks
            .Where(t => t.SprintId == sprintId)
            .ToListAsync();

        var totalDays = (int)(sprint.EndDate - sprint.StartDate).TotalDays + 1;
        var totalTasks = tasks.Count;

        // Ideal burndown
        var ideal = Enumerable.Range(0, totalDays).Select(day =>
        {
            var date = sprint.StartDate.AddDays(day);
            var remaining = (int)Math.Round(totalTasks * (1.0 - (double)day / (totalDays - 1)));
            return new BurndownPointDto(date, remaining);
        }).ToList();

        // Actual burndown — tasks completed each day
        var actual = new List<BurndownPointDto>();
        var today = DateTime.UtcNow.Date;

        for (int day = 0; day < totalDays; day++)
        {
            var date = sprint.StartDate.AddDays(day).Date;
            if (date > today) break;

            var completedByDate = tasks.Count(t =>
                t.Status == TaskStatus.Done &&
                t.UpdatedAt.HasValue &&
                t.UpdatedAt.Value.Date <= date);

            actual.Add(new BurndownPointDto(date, totalTasks - completedByDate));
        }

        return new BurndownDataDto(ideal, actual);
    }

    public async Task<SprintProgressDto> GetSprintProgressAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Sprint یافت نشد.");

        var tasks = await _context.Tasks
            .Where(t => t.SprintId == sprintId)
            .ToListAsync();

        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == TaskStatus.Done);
        var progress = total == 0 ? 0 : Math.Round((double)completed / total * 100, 1);

        var totalDays = Math.Max(1, (int)(sprint.EndDate - sprint.StartDate).TotalDays);
        var daysRemaining = Math.Max(0, (int)(sprint.EndDate - DateTime.UtcNow).TotalDays);
        var daysElapsed = totalDays - daysRemaining;

        // At risk اگه پیشرفت واقعی از ایده‌آل عقب‌تره
        var expectedProgress = totalDays == 0 ? 100 : (double)daysElapsed / totalDays * 100;
        var isAtRisk = progress < expectedProgress - 15;

        return new SprintProgressDto(
            sprint.Id, sprint.Name,
            total, completed, progress,
            totalDays, daysRemaining, isAtRisk
        );
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }
}