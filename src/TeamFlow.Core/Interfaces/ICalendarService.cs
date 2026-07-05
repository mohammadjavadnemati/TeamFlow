using TeamFlow.Core.DTOs.Calendar;

namespace TeamFlow.Core.Interfaces;

public interface ICalendarService
{
    Task<IEnumerable<CalendarEventDto>> GetDeadlinesAsync(Guid workspaceId, Guid userId, int year, int month);
    Task<IEnumerable<TimelineEventDto>> GetProjectTimelineAsync(Guid workspaceId, Guid projectId, Guid userId);
}