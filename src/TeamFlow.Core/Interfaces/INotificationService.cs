using TeamFlow.Core.DTOs.Notification;
using TeamFlow.Core.Enums;

namespace TeamFlow.Core.Interfaces;

public interface INotificationService
{
    Task CreateAsync(Guid userId, NotificationType type, string title, string message, Guid? taskId = null);
    Task<IEnumerable<NotificationDto>> GetAllAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<UnreadCountDto> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid userId, Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}