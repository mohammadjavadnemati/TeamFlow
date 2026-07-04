using TeamFlow.Core.Enums;

namespace TeamFlow.Core.DTOs.Notification;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string TypeName,
    string Title,
    string Message,
    bool IsRead,
    DateTime? ReadAt,
    Guid? TaskId,
    DateTime CreatedAt
);

public record UnreadCountDto(int Count);