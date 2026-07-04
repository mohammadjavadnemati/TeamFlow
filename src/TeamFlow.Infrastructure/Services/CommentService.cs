using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Comment;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;
    private readonly IActivityService _activityService;
    private readonly INotificationService _notificationService;

    public CommentService(
        ApplicationDbContext context,
        IWorkspaceService workspaceService,
        IActivityService activityService,
        INotificationService notificationService)
    {
        _context = context;
        _workspaceService = workspaceService;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<IEnumerable<CommentDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        return await _context.Comments
            .Where(c => c.TaskId == taskId)
            .Include(c => c.User)
            .OrderBy(c => c.CreatedAt)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<CommentDto> CreateAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CreateCommentRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var task = await _context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.Watchers)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Task یافت نشد.");

        var comment = new Comment
        {
            Content = request.Content,
            TaskId = taskId,
            UserId = userId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        await _context.Entry(comment).Reference(c => c.User).LoadAsync();

        // Activity Log
        await _activityService.LogAsync(workspaceId, userId, ActivityType.CommentAdded,
            $"کامنت روی Task '{task.Title}' ثبت شد.", projectId, taskId);

        // Notify assignee
        if (task.AssigneeId.HasValue && task.AssigneeId != userId)
        {
            await _notificationService.CreateAsync(
                task.AssigneeId.Value,
                NotificationType.CommentAdded,
                "کامنت جدید",
                $"روی Task '{task.Title}' که به شما اختصاص دارد کامنت جدیدی ثبت شد.",
                taskId
            );
        }

        // Notify watchers
        foreach (var watcher in task.Watchers.Where(w => w.UserId != userId && w.UserId != task.AssigneeId))
        {
            await _notificationService.CreateAsync(
                watcher.UserId,
                NotificationType.CommentAdded,
                "کامنت جدید",
                $"روی Task '{task.Title}' که دنبال می‌کنید کامنت جدیدی ثبت شد.",
                taskId
            );
        }

        return MapToDto(comment);
    }

    public async Task<CommentDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId, UpdateCommentRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var comment = await _context.Comments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId)
            ?? throw new KeyNotFoundException("کامنت یافت نشد.");

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("فقط نویسنده کامنت می‌تواند آن را ویرایش کند.");

        comment.Content = request.Content;
        comment.IsEdited = true;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(comment);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid commentId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");

        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId)
            ?? throw new KeyNotFoundException("کامنت یافت نشد.");

        // نویسنده یا Admin میتونن حذف کنن
        if (comment.UserId != userId && (int)role > (int)WorkspaceRole.Admin)
            throw new UnauthorizedAccessException("دسترسی کافی ندارید.");

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }

    private static CommentDto MapToDto(Comment c) => new(
        c.Id,
        c.Content,
        c.IsEdited,
        c.UserId,
        $"{c.User.FirstName} {c.User.LastName}",
        c.User.AvatarUrl,
        c.CreatedAt,
        c.UpdatedAt
    );
}