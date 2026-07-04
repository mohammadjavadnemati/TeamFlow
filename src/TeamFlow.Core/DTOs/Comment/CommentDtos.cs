using System.ComponentModel.DataAnnotations;

namespace TeamFlow.Core.DTOs.Comment;

public record CreateCommentRequest(
    [Required, StringLength(2000)] string Content
);

public record UpdateCommentRequest(
    [Required, StringLength(2000)] string Content
);

public record CommentDto(
    Guid Id,
    string Content,
    bool IsEdited,
    Guid UserId,
    string UserFullName,
    string? UserAvatarUrl,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);