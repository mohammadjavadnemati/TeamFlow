using TeamFlow.Core.DTOs.Dashboard;

namespace TeamFlow.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(Guid workspaceId, Guid userId);
}