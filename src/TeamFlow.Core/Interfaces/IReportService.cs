using TeamFlow.Core.DTOs.Report;

namespace TeamFlow.Core.Interfaces;

public interface IReportService
{
    Task<WeeklyReportDto> GetWeeklyReportAsync(Guid workspaceId, Guid userId);
    Task<WeeklyReportDto> GetCustomReportAsync(Guid workspaceId, Guid userId, DateTime from, DateTime to);
}