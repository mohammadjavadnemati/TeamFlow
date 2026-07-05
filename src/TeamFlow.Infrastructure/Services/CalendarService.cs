using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Calendar;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class CalendarService : ICalendarService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    public CalendarService(ApplicationDbContext context, IWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<IEnumerable<CalendarEventDto>> GetDeadlinesAsync(Guid workspaceId, Guid userId, int year, int month)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1).AddTicks(-1);

        var projectIds = await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync();

        return await _context.Tasks
            .Where(t =>
                projectIds.Contains(t.ProjectId) &&
                t.Deadline.HasValue &&
                t.Deadline >= from &&
                t.Deadline <= to)
            .Include(t => t.Assignee)
            .Include(t => t.Project)
            .OrderBy(t => t.Deadline)
            .Select(t => new CalendarEventDto(
                t.Id,
                t.Title,
                t.Deadline!.Value,
                t.Status,
                t.Status.ToString(),
                t.Priority,
                t.Priority.ToString(),
                t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
                t.Project.Name,
                t.Project.Color
            ))
            .ToListAsync();
    }

    public async Task<IEnumerable<TimelineEventDto>> GetProjectTimelineAsync(Guid workspaceId, Guid projectId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var activities = await _context.ActivityLogs
            .Where(a => a.ProjectId == projectId)
            .Include(a => a.User)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new TimelineEventDto(
                a.Type.ToString(),
                a.Description,
                $"{a.User.FirstName} {a.User.LastName}",
                a.User.AvatarUrl,
                a.CreatedAt
            ))
            .ToListAsync();

        return activities;
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }
}