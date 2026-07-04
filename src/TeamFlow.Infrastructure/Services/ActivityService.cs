using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Activity;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;

    public ActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(Guid workspaceId, Guid userId, ActivityType type, string description,
        Guid? projectId = null, Guid? taskId = null, string? metadata = null)
    {
        var log = new ActivityLog
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Type = type,
            Description = description,
            ProjectId = projectId,
            TaskId = taskId,
            Metadata = metadata
        };

        _context.ActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ActivityLogDto>> GetWorkspaceActivitiesAsync(Guid workspaceId, Guid userId, int page = 1, int pageSize = 20)
    {
        return await _context.ActivityLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .Include(a => a.User)
            .Include(a => a.Task)
            .Include(a => a.Project)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    public async Task<IEnumerable<ActivityLogDto>> GetTaskActivitiesAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        return await _context.ActivityLogs
            .Where(a => a.TaskId == taskId)
            .Include(a => a.User)
            .Include(a => a.Task)
            .Include(a => a.Project)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => MapToDto(a))
            .ToListAsync();
    }

    private static ActivityLogDto MapToDto(ActivityLog a) => new(
        a.Id,
        a.Type,
        a.Type.ToString(),
        a.Description,
        $"{a.User.FirstName} {a.User.LastName}",
        a.User.AvatarUrl,
        a.TaskId,
        a.Task?.Title,
        a.ProjectId,
        a.Project?.Name,
        a.CreatedAt
    );
}