using TeamFlow.Core.DTOs.Smart;

namespace TeamFlow.Core.Interfaces;

public interface ISmartService
{
    Task<WorkloadAnalysisDto> GetWorkloadAnalysisAsync(Guid workspaceId, Guid userId);
    Task<DeadlineRiskDto> GetDeadlineRiskAsync(Guid workspaceId, Guid projectId, Guid sprintId, Guid userId);
    Task<IEnumerable<ProductivityScoreDto>> GetProductivityScoresAsync(Guid workspaceId, Guid userId);
    Task<TeamStatisticsDto> GetTeamStatisticsAsync(Guid workspaceId, Guid userId);
    Task<DailyStandupDto> GetDailyStandupAsync(Guid workspaceId, Guid userId);
}