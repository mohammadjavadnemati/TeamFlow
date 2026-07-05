using TeamFlow.Core.Entities;
using TeamFlow.Core.DTOs.Task;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    private readonly INotificationService _notificationService;
    

    public TaskService(
        ApplicationDbContext context,
        IWorkspaceService workspaceService,
        INotificationService notificationService)
    {
        _context = context;
        _workspaceService = workspaceService;
        _notificationService = notificationService;
    }

    // ─── Task CRUD ────────────────────────────────────────────────────────────

    public async Task<TaskDto> CreateAsync(Guid workspaceId, Guid projectId, Guid userId, CreateTaskRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetProjectOrThrowAsync(workspaceId, projectId);

        var task = new ProjectTask
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            StoryPoints = request.StoryPoints,
            Deadline = request.Deadline,
            EstimatedTime = request.EstimatedTime,
            SprintId = request.SprintId,
            AssigneeId = request.AssigneeId,
            ProjectId = projectId,
            CreatedById = userId
        };

        _context.Tasks.Add(task);

        if (request.LabelIds?.Any() == true)
        {
            foreach (var labelId in request.LabelIds)
                _context.TaskLabels.Add(new TaskLabel { Task = task, LabelId = labelId });
        }

        await _context.SaveChangesAsync();
        return await GetTaskDtoAsync(task.Id, userId);
    }

    public async Task<IEnumerable<TaskDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid userId, TaskFilterRequest filter)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetProjectOrThrowAsync(workspaceId, projectId);

        var query = _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Sprint)
            .Include(t => t.Subtasks)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(t => t.Watchers)
            .Include(t => t.Bookmarks)
            .AsQueryable();

        // Filters
        if (filter.Status.HasValue) query = query.Where(t => t.Status == filter.Status);
        if (filter.Priority.HasValue) query = query.Where(t => t.Priority == filter.Priority);
        if (filter.SprintId.HasValue) query = query.Where(t => t.SprintId == filter.SprintId);
        if (filter.AssigneeId.HasValue) query = query.Where(t => t.AssigneeId == filter.AssigneeId);
        if (filter.LabelId.HasValue) query = query.Where(t => t.TaskLabels.Any(tl => tl.LabelId == filter.LabelId));
        if (filter.DeadlineFrom.HasValue) query = query.Where(t => t.Deadline >= filter.DeadlineFrom);
        if (filter.DeadlineTo.HasValue) query = query.Where(t => t.Deadline <= filter.DeadlineTo);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(s) ||
                (t.Description != null && t.Description.ToLower().Contains(s)));
        }

        var tasks = await query.ToListAsync();
        return tasks.Select(t => MapToDto(t, userId));
    }

    public async Task<TaskDetailDto> GetByIdAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var task = await _context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Sprint)
            .Include(t => t.Subtasks).ThenInclude(s => s.Assignee)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(t => t.Checklists).ThenInclude(c => c.Items)
            .Include(t => t.Watchers).ThenInclude(w => w.User)
            .Include(t => t.Bookmarks)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Task یافت نشد.");

        return MapToDetailDto(task, userId);
    }

    public async Task<TaskDto> UpdateAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, UpdateTaskRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var task = await _context.Tasks
            .Include(t => t.TaskLabels)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Sprint)
            .Include(t => t.Subtasks)
            .Include(t => t.Watchers)
            .Include(t => t.Bookmarks)
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Task یافت نشد.");

        task.Title = request.Title ?? task.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.StoryPoints = request.StoryPoints;
        task.Deadline = request.Deadline;
        task.EstimatedTime = request.EstimatedTime;
        task.ActualTime = request.ActualTime;
        task.SprintId = request.SprintId;
        task.AssigneeId = request.AssigneeId;
        task.UpdatedAt = DateTime.UtcNow;

        // Labels sync
        _context.TaskLabels.RemoveRange(task.TaskLabels);
        if (request.LabelIds?.Any() == true)
        {
            foreach (var labelId in request.LabelIds)
                _context.TaskLabels.Add(new TaskLabel { TaskId = taskId, LabelId = labelId });
        }

        await _context.SaveChangesAsync();
        return await GetTaskDtoAsync(taskId, userId);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.Developer);

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Task یافت نشد.");

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
    }

    // ─── Subtasks ─────────────────────────────────────────────────────────────

    public async Task<SubtaskDto> CreateSubtaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CreateSubtaskRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetTaskOrThrowAsync(projectId, taskId);

        var subtask = new Subtask
        {
            Title = request.Title,
            AssigneeId = request.AssigneeId,
            TaskId = taskId
        };

        _context.Subtasks.Add(subtask);
        await _context.SaveChangesAsync();

        await _context.Entry(subtask).Reference(s => s.Assignee).LoadAsync();
        return MapSubtaskToDto(subtask);
    }

    public async Task<SubtaskDto> UpdateSubtaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid subtaskId, Guid userId, UpdateSubtaskRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetTaskOrThrowAsync(projectId, taskId);

        var subtask = await _context.Subtasks
            .Include(s => s.Assignee)
            .FirstOrDefaultAsync(s => s.Id == subtaskId && s.TaskId == taskId)
            ?? throw new KeyNotFoundException("Subtask یافت نشد.");

        subtask.Title = request.Title ?? subtask.Title;
        subtask.AssigneeId = request.AssigneeId ?? subtask.AssigneeId;
        subtask.UpdatedAt = DateTime.UtcNow;

        var isCompleted = request.IsCompleted ?? subtask.IsCompleted;

        if (isCompleted && !subtask.IsCompleted)
            subtask.CompletedAt = DateTime.UtcNow;
        else if (!isCompleted)
            subtask.CompletedAt = null;

        subtask.IsCompleted = isCompleted;

        await _context.SaveChangesAsync();
        return MapSubtaskToDto(subtask);
    }

    public async Task DeleteSubtaskAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid subtaskId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetTaskOrThrowAsync(projectId, taskId);

        var subtask = await _context.Subtasks
            .FirstOrDefaultAsync(s => s.Id == subtaskId && s.TaskId == taskId)
            ?? throw new KeyNotFoundException("Subtask یافت نشد.");

        _context.Subtasks.Remove(subtask);
        await _context.SaveChangesAsync();
    }

    // ─── Labels ───────────────────────────────────────────────────────────────

    public async Task<LabelDto> CreateLabelAsync(Guid workspaceId, Guid userId, CreateLabelRequest request)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.Developer);

        var label = new Label
        {
            Name = request.Name,
            Color = request.Color,
            WorkspaceId = workspaceId
        };

        _context.Labels.Add(label);
        await _context.SaveChangesAsync();

        return new LabelDto(label.Id, label.Name, label.Color);
    }

    public async Task<IEnumerable<LabelDto>> GetLabelsAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        return await _context.Labels
            .Where(l => l.WorkspaceId == workspaceId)
            .Select(l => new LabelDto(l.Id, l.Name, l.Color))
            .ToListAsync();
    }

    public async Task DeleteLabelAsync(Guid workspaceId, Guid labelId, Guid userId)
    {
        await EnsureRoleAsync(workspaceId, userId, WorkspaceRole.Admin);

        var label = await _context.Labels
            .FirstOrDefaultAsync(l => l.Id == labelId && l.WorkspaceId == workspaceId)
            ?? throw new KeyNotFoundException("Label یافت نشد.");

        _context.Labels.Remove(label);
        await _context.SaveChangesAsync();
    }

    // ─── Checklists ───────────────────────────────────────────────────────────

    public async Task<ChecklistDto> CreateChecklistAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, CreateChecklistRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetTaskOrThrowAsync(projectId, taskId);

        var checklist = new Checklist { Title = request.Title, TaskId = taskId };
        _context.Checklists.Add(checklist);
        await _context.SaveChangesAsync();

        return new ChecklistDto(checklist.Id, checklist.Title, new List<ChecklistItemDto>(), 0, 0);
    }

    public async Task DeleteChecklistAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetTaskOrThrowAsync(projectId, taskId);

        var checklist = await _context.Checklists
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TaskId == taskId)
            ?? throw new KeyNotFoundException("Checklist یافت نشد.");

        _context.Checklists.Remove(checklist);
        await _context.SaveChangesAsync();
    }

    public async Task<ChecklistItemDto> AddChecklistItemAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid userId, CreateChecklistItemRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var checklist = await _context.Checklists
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == checklistId && c.TaskId == taskId)
            ?? throw new KeyNotFoundException("Checklist یافت نشد.");

        var item = new ChecklistItem
        {
            Title = request.Title,
            ChecklistId = checklistId,
            Order = checklist.Items.Count
        };

        _context.ChecklistItems.Add(item);
        await _context.SaveChangesAsync();

        return new ChecklistItemDto(item.Id, item.Title, item.IsChecked, item.Order);
    }

    public async Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid itemId, Guid userId, UpdateChecklistItemRequest request)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var item = await _context.ChecklistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ChecklistId == checklistId)
            ?? throw new KeyNotFoundException("آیتم یافت نشد.");

        item.Title = request.Title ?? item.Title;
        item.IsChecked = request.IsChecked ?? item.IsChecked;
        item.Order = request.Order ?? item.Order;
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return new ChecklistItemDto(item.Id, item.Title, item.IsChecked, item.Order);
    }

    public async Task DeleteChecklistItemAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid checklistId, Guid itemId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var item = await _context.ChecklistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.ChecklistId == checklistId)
            ?? throw new KeyNotFoundException("آیتم یافت نشد.");

        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync();
    }

    // ─── Watchers & Bookmarks ─────────────────────────────────────────────────

    public async Task WatchAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetTaskOrThrowAsync(projectId, taskId);

        var exists = await _context.TaskWatchers.AnyAsync(w => w.TaskId == taskId && w.UserId == userId);
        if (exists) return;

        _context.TaskWatchers.Add(new TaskWatcher { TaskId = taskId, UserId = userId });
        await _context.SaveChangesAsync();
    }

    public async Task UnwatchAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        var watcher = await _context.TaskWatchers
            .FirstOrDefaultAsync(w => w.TaskId == taskId && w.UserId == userId);

        if (watcher is null) return;

        _context.TaskWatchers.Remove(watcher);
        await _context.SaveChangesAsync();
    }

    public async Task BookmarkAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);
        await GetTaskOrThrowAsync(projectId, taskId);

        var exists = await _context.TaskBookmarks.AnyAsync(b => b.TaskId == taskId && b.UserId == userId);
        if (exists) return;

        _context.TaskBookmarks.Add(new TaskBookmark { TaskId = taskId, UserId = userId });
        await _context.SaveChangesAsync();
    }

    public async Task UnbookmarkAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        var bookmark = await _context.TaskBookmarks
            .FirstOrDefaultAsync(b => b.TaskId == taskId && b.UserId == userId);

        if (bookmark is null) return;

        _context.TaskBookmarks.Remove(bookmark);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<TaskDto>> GetBookmarksAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var tasks = await _context.TaskBookmarks
            .Where(b => b.UserId == userId && b.Task.Project.WorkspaceId == workspaceId)
            .Include(b => b.Task).ThenInclude(t => t.Assignee)
            .Include(b => b.Task).ThenInclude(t => t.CreatedBy)
            .Include(b => b.Task).ThenInclude(t => t.Sprint)
            .Include(b => b.Task).ThenInclude(t => t.Subtasks)
            .Include(b => b.Task).ThenInclude(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(b => b.Task).ThenInclude(t => t.Watchers)
            .Include(b => b.Task).ThenInclude(t => t.Bookmarks)
            .Select(b => b.Task)
            .ToListAsync();

        return tasks.Select(t => MapToDto(t, userId));
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

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
            throw new UnauthorizedAccessException("سطح دسترسی شما کافی نیست.");
    }

    private async Task<Project> GetProjectOrThrowAsync(Guid workspaceId, Guid projectId)
    {
        return await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.WorkspaceId == workspaceId)
            ?? throw new KeyNotFoundException("پروژه یافت نشد.");
    }

    private async Task<ProjectTask> GetTaskOrThrowAsync(Guid projectId, Guid taskId)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Task یافت نشد.");
    }

    private async Task<TaskDto> GetTaskDtoAsync(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Sprint)
            .Include(t => t.Subtasks)
            .Include(t => t.TaskLabels).ThenInclude(tl => tl.Label)
            .Include(t => t.Watchers)
            .Include(t => t.Bookmarks)
            .FirstAsync(t => t.Id == taskId);

        return MapToDto(task, userId);
    }

    private static TaskDto MapToDto(ProjectTask t, Guid userId) => new(
        t.Id, t.Title, t.Description,
        t.Status, t.Status.ToString(),
        t.Priority, t.Priority.ToString(),
        t.StoryPoints, t.Deadline, t.EstimatedTime, t.ActualTime,
        t.ProjectId, t.SprintId, t.Sprint?.Name,
        t.Assignee is null ? null : new AssigneeDto(t.Assignee.Id, t.Assignee.FirstName, t.Assignee.LastName, t.Assignee.Email!, t.Assignee.AvatarUrl),
        $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}",
        t.Subtasks.Count,
        t.Subtasks.Count(s => s.IsCompleted),
        t.TaskLabels.Select(tl => new LabelDto(tl.Label.Id, tl.Label.Name, tl.Label.Color)),
        t.Watchers.Any(w => w.UserId == userId),
        t.Bookmarks.Any(b => b.UserId == userId),
        t.CreatedAt
    );

    private static TaskDetailDto MapToDetailDto(ProjectTask t, Guid userId) => new(
        t.Id, t.Title, t.Description,
        t.Status, t.Status.ToString(),
        t.Priority, t.Priority.ToString(),
        t.StoryPoints, t.Deadline, t.EstimatedTime, t.ActualTime,
        t.ProjectId, t.SprintId, t.Sprint?.Name,
        t.Assignee is null ? null : new AssigneeDto(t.Assignee.Id, t.Assignee.FirstName, t.Assignee.LastName, t.Assignee.Email!, t.Assignee.AvatarUrl),
        $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}",
        t.Subtasks.Select(MapSubtaskToDto),
        t.TaskLabels.Select(tl => new LabelDto(tl.Label.Id, tl.Label.Name, tl.Label.Color)),
        t.Checklists.Select(c => new ChecklistDto(
            c.Id, c.Title,
            c.Items.OrderBy(i => i.Order).Select(i => new ChecklistItemDto(i.Id, i.Title, i.IsChecked, i.Order)),
            c.Items.Count,
            c.Items.Count(i => i.IsChecked)
        )),
        t.Watchers.Select(w => new WatcherDto(w.User.Id, w.User.FirstName, w.User.LastName, w.User.AvatarUrl)),
        t.Watchers.Any(w => w.UserId == userId),
        t.Bookmarks.Any(b => b.UserId == userId),
        t.CreatedAt
    );

    private static SubtaskDto MapSubtaskToDto(Subtask s) => new(
        s.Id, s.Title, s.IsCompleted, s.CompletedAt,
        s.Assignee is null ? null : new AssigneeDto(s.Assignee.Id, s.Assignee.FirstName, s.Assignee.LastName, s.Assignee.Email!, s.Assignee.AvatarUrl)
    );
    public async Task AssignAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid requesterId, AssignTaskRequest request)
    {
        await EnsureMemberAsync(workspaceId, requesterId);

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Task یافت نشد.");

        // چک کن assignee عضو workspace باشه
        if (request.AssigneeId.HasValue)
        {
            var isMember = await _context.WorkspaceMembers
                .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == request.AssigneeId);

            if (!isMember)
                throw new InvalidOperationException("کاربر مورد نظر عضو این Workspace نیست.");
        }

        var previousAssigneeId = task.AssigneeId;
        task.AssigneeId = request.AssigneeId;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notification به assignee جدید
        if (request.AssigneeId.HasValue && request.AssigneeId != previousAssigneeId)
        {
            await _context.Entry(task).Reference(t => t.Project).LoadAsync();

            await _notificationService.CreateAsync(
                request.AssigneeId.Value,
                NotificationType.TaskAssigned,
                "Task جدید به شما اختصاص یافت",
                $"Task '{task.Title}' در پروژه '{task.Project.Name}' به شما اختصاص داده شد.",
                taskId
            );
        }
    }
}