using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Report;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    public ReportService(ApplicationDbContext context, IWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<WeeklyReportDto> GetWeeklyReportAsync(Guid workspaceId, Guid userId)
    {
        var to = DateTime.UtcNow;
        var from = to.AddDays(-7);
        return await GetCustomReportAsync(workspaceId, userId, from, to);
    }

    public async Task<WeeklyReportDto> GetCustomReportAsync(Guid workspaceId, Guid userId, DateTime from, DateTime to)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var projectIds = await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync();

        var allTasks = await _context.Tasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .Include(t => t.Assignee)
            .ToListAsync();

        var completedInRange = allTasks.Where(t =>
            t.Status == TaskStatus.Done &&
            t.UpdatedAt >= from && t.UpdatedAt <= to).ToList();

        var newInRange = allTasks.Where(t =>
            t.CreatedAt >= from && t.CreatedAt <= to).ToList();

        var delayed = allTasks.Where(t =>
            t.Deadline.HasValue &&
            t.Deadline < DateTime.UtcNow &&
            t.Status != TaskStatus.Done).ToList();

        // میانگین روزهای تکمیل
        var completionDays = completedInRange
            .Where(t => t.UpdatedAt.HasValue)
            .Select(t => (t.UpdatedAt!.Value - t.CreatedAt).TotalDays)
            .ToList();
        var avgDays = completionDays.Any() ? Math.Round(completionDays.Average(), 1) : 0;

        // Sprint progress فعال
        var activeSprint = await _context.Sprints
            .Where(s => s.Status == SprintStatus.Active && projectIds.Contains(s.ProjectId))
            .FirstOrDefaultAsync();

        double sprintProgress = 0;
        if (activeSprint != null)
        {
            var sprintTasks = allTasks.Where(t => t.SprintId == activeSprint.Id).ToList();
            sprintProgress = sprintTasks.Count == 0 ? 0 :
                Math.Round((double)sprintTasks.Count(t => t.Status == TaskStatus.Done) / sprintTasks.Count * 100, 1);
        }

        // Productivity per user
        var members = await _context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Include(m => m.User)
            .ToListAsync();

        var userProductivities = members.Select(m =>
        {
            var userCompleted = completedInRange.Where(t => t.AssigneeId == m.UserId).ToList();
            var userDelayed = delayed.Where(t => t.AssigneeId == m.UserId).ToList();

            var userAvgDays = userCompleted
                .Where(t => t.UpdatedAt.HasValue)
                .Select(t => (t.UpdatedAt!.Value - t.CreatedAt).TotalDays)
                .DefaultIfEmpty(0)
                .Average();

            // فرمول: تعداد تکمیل‌شده * 10 - تعداد تاخیر * 5 - میانگین روز * 0.5
            var score = Math.Max(0, Math.Min(100,
                userCompleted.Count * 10 -
                userDelayed.Count * 5 -
                userAvgDays * 0.5));

            return new UserProductivityDto(
                m.UserId,
                $"{m.User.FirstName} {m.User.LastName}",
                m.User.AvatarUrl,
                userCompleted.Count,
                userDelayed.Count,
                Math.Round(userAvgDays, 1),
                Math.Round(score, 1)
            );
        });

        return new WeeklyReportDto(
            from, to,
            completedInRange.Count,
            allTasks.Count(t => t.Status == TaskStatus.Blocked),
            newInRange.Count,
            delayed.Count,
            avgDays,
            sprintProgress,
            userProductivities
        );
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }
}