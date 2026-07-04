using TeamFlow.Core.Enums;

namespace TeamFlow.Core.DTOs.Activity;

public record ActivityLogDto(
    Guid Id,
    ActivityType Type,
    string TypeName,
    string Description,
    string UserFullName,
    string? UserAvatarUrl,
    Guid? TaskId,
    string? TaskTitle,
    Guid? ProjectId,
    string? ProjectName,
    DateTime CreatedAt
);