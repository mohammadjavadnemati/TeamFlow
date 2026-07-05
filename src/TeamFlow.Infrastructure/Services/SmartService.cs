using Microsoft.EntityFrameworkCore;
using TeamFlow.Core.DTOs.Smart;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;
using TaskStatus = TeamFlow.Core.Enums.TaskStatus;

namespace TeamFlow.Infrastructure.Services;

public class SmartService : ISmartService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;

    private const int OverloadThreshold = 10;

    public SmartService(ApplicationDbContext context, IWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<WorkloadAnalysisDto> GetWorkloadAnalysisAsync(Guid workspaceId, Guid userId)
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
            .Where(t => projectIds.Contains(t.ProjectId) &&
                        t.Status != TaskStatus.Done &&
                        t.AssigneeId != null)
            .ToListAsync();

        var userWorkloads = members.Select(m =>
        {
            var userTasks = tasks.Where(t => t.AssigneeId == m.UserId).ToList();
            var active = userTasks.Count(t => t.Status == TaskStatus.InProgress);
            var blocked = userTasks.Count(t => t.Status == TaskStatus.Blocked);
            var overdue = userTasks.Count(t => t.Deadline.HasValue && t.Deadline < DateTime.UtcNow);
            var total = userTasks.Count;
            var isOverloaded = total >= OverloadThreshold;

            var (level, emoji) = total switch
            {
                0 => ("Free", "🟢"),
                <= 4 => ("Light", "🟢"),
                <= 7 => ("Moderate", "🟡"),
                <= 9 => ("Heavy", "🟠"),
                _ => ("Overloaded", "🔴")
            };

            return new UserWorkloadDto(
                m.UserId,
                $"{m.User.FirstName} {m.User.LastName}",
                m.User.AvatarUrl,
                active, blocked, overdue,
                isOverloaded, level, emoji
            );
        }).ToList();

        var overloadedCount = userWorkloads.Count(u => u.IsOverloaded);
        var summary = overloadedCount == 0
            ? "✅ توزیع کار تیم در وضعیت مناسبی قرار دارد."
            : $"⚠️ {overloadedCount} نفر از اعضای تیم بیش از حد مشغول هستند.";

        return new WorkloadAnalysisDto(userWorkloads, summary);
    }

    public async Task<DeadlineRiskDto> GetDeadlineRiskAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var sprint = await _context.Sprints
            .FirstOrDefaultAsync(s => s.Id == sprintId && s.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Sprint یافت نشد.");

        var tasks = await _context.Tasks
            .Where(t => t.SprintId == sprintId)
            .Include(t => t.Assignee)
            .ToListAsync();

        var total = tasks.Count;
        var completed = tasks.Count(t => t.Status == TaskStatus.Done);
        var daysRemaining = Math.Max(0, (int)(sprint.EndDate - DateTime.UtcNow).TotalDays);
        var totalDays = Math.Max(1, (int)(sprint.EndDate - sprint.StartDate).TotalDays);
        var daysElapsed = totalDays - daysRemaining;

        var currentProgress = total == 0 ? 100 : (double)completed / total * 100;
        var requiredProgress = (double)daysElapsed / totalDays * 100;
        var gap = requiredProgress - currentProgress;

        var (riskLevel, riskEmoji, isAtRisk) = gap switch
        {
            <= 0 => ("Low", "🟢", false),
            <= 15 => ("Medium", "🟡", true),
            <= 30 => ("High", "🟠", true),
            _ => ("Critical", "🔴", true)
        };

        var message = isAtRisk
            ? $"⚠️ {riskLevel} Risk — این Sprint احتمالاً به موقع تمام نمی‌شود. پیشرفت فعلی {currentProgress:F0}% ولی باید {requiredProgress:F0}% می‌بود."
            : $"✅ Sprint در مسیر درستی قرار دارد. پیشرفت: {currentProgress:F0}%";

        var riskyTasks = tasks
            .Where(t => t.Status != TaskStatus.Done &&
                        t.Deadline.HasValue &&
                        t.Deadline < DateTime.UtcNow.AddDays(3))
            .Select(t => new RiskyTaskDto(
                t.Id, t.Title, t.Deadline!.Value,
                (int)(t.Deadline.Value - DateTime.UtcNow).TotalDays,
                t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null
            ));

        return new DeadlineRiskDto(
            sprint.Id, sprint.Name, isAtRisk,
            riskLevel, riskEmoji, message,
            Math.Round(currentProgress, 1),
            Math.Round(requiredProgress, 1),
            daysRemaining, riskyTasks
        );
    }

    public async Task<IEnumerable<ProductivityScoreDto>> GetProductivityScoresAsync(Guid workspaceId, Guid userId)
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
            .Where(t => projectIds.Contains(t.ProjectId))
            .ToListAsync();

        return members.Select(m =>
        {
            var userTasks = tasks.Where(t => t.AssigneeId == m.UserId).ToList();
            var completed = userTasks.Where(t => t.Status == TaskStatus.Done).ToList();
            var delayed = userTasks.Where(t =>
                t.Deadline.HasValue && t.Deadline < DateTime.UtcNow &&
                t.Status != TaskStatus.Done).ToList();

            var avgDays = completed
                .Where(t => t.UpdatedAt.HasValue)
                .Select(t => (t.UpdatedAt!.Value - t.CreatedAt).TotalDays)
                .DefaultIfEmpty(0)
                .Average();

            // فرمول امتیاز بهره‌وری
            var score = Math.Max(0, Math.Min(100,
                completed.Count * 8 -
                delayed.Count * 6 -
                avgDays * 0.3
            ));

            var (label, insight) = score switch
            {
                >= 80 => ("Excellent", "عملکرد فوق‌العاده! ادامه بده."),
                >= 60 => ("Good", "عملکرد خوبی داری، کمی بهبود ممکنه."),
                >= 40 => ("Average", "Task های معوق رو اولویت‌بندی کن."),
                _ => ("Needs Improvement", "نیاز به بررسی و بهبود جدی وجود داره.")
            };

            return new ProductivityScoreDto(
                m.UserId,
                $"{m.User.FirstName} {m.User.LastName}",
                m.User.AvatarUrl,
                Math.Round(score, 1),
                label,
                completed.Count,
                Math.Round(avgDays, 1),
                delayed.Count,
                insight
            );
        }).OrderByDescending(p => p.Score);
    }

    public async Task<TeamStatisticsDto> GetTeamStatisticsAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var projectIds = await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync();

        var tasks = await _context.Tasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .ToListAsync();

        var taskIds = tasks.Select(t => t.Id).ToList();

        var comments = await _context.Comments
            .Where(c => taskIds.Contains(c.TaskId))
            .ToListAsync();

        var members = await _context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Include(m => m.User)
            .ToListAsync();

        // فعال‌ترین عضو (بیشترین Task فعال)
        var mostActive = members
            .Select(m => new
            {
                Member = m,
                Count = tasks.Count(t => t.AssigneeId == m.UserId && t.Status != TaskStatus.Done)
            })
            .OrderByDescending(x => x.Count)
            .First();

        // بیشترین Task تکمیل شده
        var mostCompleted = members
            .Select(m => new
            {
                Member = m,
                Count = tasks.Count(t => t.AssigneeId == m.UserId && t.Status == TaskStatus.Done)
            })
            .OrderByDescending(x => x.Count)
            .First();

        // بیشترین کامنت
        var mostComments = members
            .Select(m => new
            {
                Member = m,
                Count = comments.Count(c => c.UserId == m.UserId)
            })
            .OrderByDescending(x => x.Count)
            .First();

        // بیشترین Bug رفع شده (Label = Bug)
        var bugTaskIds = await _context.TaskLabels
            .Where(tl => taskIds.Contains(tl.TaskId))
            .Include(tl => tl.Label)
            .Where(tl => tl.Label.Name.ToLower() == "bug")
            .Select(tl => tl.TaskId)
            .ToListAsync();

        var mostBugsFixed = members
            .Select(m => new
            {
                Member = m,
                Count = tasks.Count(t =>
                    bugTaskIds.Contains(t.Id) &&
                    t.AssigneeId == m.UserId &&
                    t.Status == TaskStatus.Done)
            })
            .OrderByDescending(x => x.Count)
            .First();

        var productivityScores = await GetProductivityScoresAsync(workspaceId, userId);

        return new TeamStatisticsDto(
            new MostActiveUserDto(mostActive.Member.UserId,
                $"{mostActive.Member.User.FirstName} {mostActive.Member.User.LastName}",
                mostActive.Member.User.AvatarUrl, mostActive.Count, "Task فعال"),
            new MostActiveUserDto(mostCompleted.Member.UserId,
                $"{mostCompleted.Member.User.FirstName} {mostCompleted.Member.User.LastName}",
                mostCompleted.Member.User.AvatarUrl, mostCompleted.Count, "Task تکمیل‌شده"),
            new MostActiveUserDto(mostComments.Member.UserId,
                $"{mostComments.Member.User.FirstName} {mostComments.Member.User.LastName}",
                mostComments.Member.User.AvatarUrl, mostComments.Count, "کامنت"),
            new MostActiveUserDto(mostBugsFixed.Member.UserId,
                $"{mostBugsFixed.Member.User.FirstName} {mostBugsFixed.Member.User.LastName}",
                mostBugsFixed.Member.User.AvatarUrl, mostBugsFixed.Count, "Bug رفع‌شده"),
            productivityScores
        );
    }

    public async Task<DailyStandupDto> GetDailyStandupAsync(Guid workspaceId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var today = DateTime.UtcNow.Date;

        var projectIds = await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId)
            .Select(p => p.Id)
            .ToListAsync();

        var allTasks = await _context.Tasks
            .Where(t => projectIds.Contains(t.ProjectId))
            .Include(t => t.Assignee)
            .ToListAsync();

        var taskIds = allTasks.Select(t => t.Id).ToList();

        // Bug های جدید امروز
        var newBugTaskIds = await _context.TaskLabels
            .Where(tl => taskIds.Contains(tl.TaskId))
            .Include(tl => tl.Label)
            .Where(tl => tl.Label.Name.ToLower() == "bug")
            .Select(tl => tl.TaskId)
            .ToListAsync();

        var completedToday = allTasks.Count(t =>
            t.Status == TaskStatus.Done &&
            t.UpdatedAt?.Date == today);

        var newBugs = allTasks.Count(t =>
            newBugTaskIds.Contains(t.Id) &&
            t.CreatedAt.Date == today);

        var delayed = allTasks.Count(t =>
            t.Deadline.HasValue &&
            t.Deadline < DateTime.UtcNow &&
            t.Status != TaskStatus.Done);

        // Sprint فعال
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

        var sprintLabel = sprintProgress switch
        {
            >= 80 => "🟢 عالی",
            >= 60 => "🟡 خوب",
            >= 40 => "🟠 نیاز به توجه",
            _ => "🔴 عقب‌افتاده"
        };

        // خلاصه هر عضو
        var members = await _context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId)
            .Include(m => m.User)
            .ToListAsync();

        var userUpdates = members
            .Select(m =>
            {
                var completedTodayByUser = allTasks
                    .Where(t => t.AssigneeId == m.UserId &&
                                t.Status == TaskStatus.Done &&
                                t.UpdatedAt?.Date == today)
                    .ToList();

                var inProgress = allTasks.Count(t =>
                    t.AssigneeId == m.UserId &&
                    t.Status == TaskStatus.InProgress);

                return new StandupUserDto(
                    $"{m.User.FirstName} {m.User.LastName}",
                    m.User.AvatarUrl,
                    completedTodayByUser.Count,
                    inProgress,
                    completedTodayByUser.Select(t => t.Title)
                );
            })
            .Where(u => u.CompletedToday > 0 || u.InProgress > 0);

        return new DailyStandupDto(
            today,
            completedToday,
            newBugs,
            delayed,
            sprintProgress,
            sprintLabel,
            userUpdates
        );
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }
}