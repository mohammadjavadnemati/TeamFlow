using TeamFlow.Core.DTOs.Task;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Entities;
using TeamFlow.Infrastructure.Data;
using TeamFlow.Infrastructure.Services;
using TeamFlow.Tests.Helpers;
using Xunit;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;
using System.Threading.Tasks;

namespace TeamFlow.Tests.Tasks; // ← از Task به Tasks


public class TaskServiceTests
{
    private readonly TaskService _taskService;
    private readonly ApplicationDbContext _context;
    private readonly ApplicationUser _owner;
    private readonly Guid _workspaceId;
    private readonly Guid _projectId;

    public TaskServiceTests()
    {
        _context = TestDbContextFactory.Create();
        var userManager = TestDbContextFactory.CreateUserManager(_context);

        _owner = TestDbContextFactory.MakeUser("Owner", "User", "owner@test.com");
        _context.Users.Add(_owner);

        var workspace = new Core.Entities.Workspace { Name = "Test Workspace" };
        _context.Workspaces.Add(workspace);
        _workspaceId = workspace.Id;

        _context.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = _owner.Id,
            Role = WorkspaceRole.Owner
        });

        var project = new Project
        {
            Name = "Test Project",
            WorkspaceId = workspace.Id,
            CreatedById = _owner.Id
        };
        _context.Projects.Add(project);
        _projectId = project.Id;

        _context.SaveChanges();

        var workspaceService = new WorkspaceService(_context, userManager);
        var notificationService = TestDbContextFactory.CreateMockNotificationService(); // ← اضافه شد
        _taskService = new TaskService(_context, workspaceService, notificationService); // ← اضافه شد
        // _taskService = new TaskService(_context, workspaceService);
    }

    [Fact]
    public async Task CreateTask_ValidData_ReturnsTask()
    {
        var request = new CreateTaskRequest(
            "Task اول",
            "توضیحات",
            TaskStatus.Todo,
            TaskPriority.High
        );

        var result = await _taskService.CreateAsync(_workspaceId, _projectId, _owner.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Task اول", result.Title);
        Assert.Equal(TaskPriority.High, result.Priority);
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredTasks()
    {
        await _taskService.CreateAsync(_workspaceId, _projectId, _owner.Id,
            new CreateTaskRequest("Task Todo", null, TaskStatus.Todo, TaskPriority.Low));

        await _taskService.CreateAsync(_workspaceId, _projectId, _owner.Id,
            new CreateTaskRequest("Task Done", null, TaskStatus.Done, TaskPriority.Low));

        var filter = new TaskFilterRequest(Status: TaskStatus.Todo);
        var result = await _taskService.GetAllAsync(_workspaceId, _projectId, _owner.Id, filter);

        Assert.Single(result);
        Assert.Equal(TaskStatus.Todo, result.First().Status);
    }

    [Fact]
    public async Task BookmarkTask_ThenGetBookmarks_ReturnsTask()
    {
        var task = await _taskService.CreateAsync(_workspaceId, _projectId, _owner.Id,
            new CreateTaskRequest("Bookmarked Task", null, TaskStatus.Todo, TaskPriority.Medium));

        await _taskService.BookmarkAsync(_workspaceId, _projectId, task.Id, _owner.Id);

        var bookmarks = await _taskService.GetBookmarksAsync(_workspaceId, _owner.Id);

        Assert.Single(bookmarks);
        Assert.Equal(task.Id, bookmarks.First().Id);
    }

    [Fact]
    public async Task WatchTask_IsWatching_ReturnsTrue()
    {
        var task = await _taskService.CreateAsync(_workspaceId, _projectId, _owner.Id,
            new CreateTaskRequest("Watched Task", null, TaskStatus.Todo, TaskPriority.Medium));

        await _taskService.WatchAsync(_workspaceId, _projectId, task.Id, _owner.Id);

        var detail = await _taskService.GetByIdAsync(_workspaceId, _projectId, task.Id, _owner.Id);

        Assert.True(detail.IsWatching);
    }

    [Fact]
    public async Task CreateSubtask_ValidData_SubtaskAdded()
    {
        var task = await _taskService.CreateAsync(_workspaceId, _projectId, _owner.Id,
            new CreateTaskRequest("Parent Task", null, TaskStatus.Todo, TaskPriority.Medium));

        var subtask = await _taskService.CreateSubtaskAsync(
            _workspaceId, _projectId, task.Id, _owner.Id,
            new CreateSubtaskRequest("Subtask اول"));

        Assert.NotNull(subtask);
        Assert.Equal("Subtask اول", subtask.Title);
        Assert.False(subtask.IsCompleted);
    }
}