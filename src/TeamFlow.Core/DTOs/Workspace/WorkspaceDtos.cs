using System.ComponentModel.DataAnnotations;
using TeamFlow.Core.Enums;

namespace TeamFlow.Core.DTOs.Workspace;

public record CreateWorkspaceRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(500)] string? Description,
    string? LogoUrl
);

public record UpdateWorkspaceRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(500)] string? Description,
    string? LogoUrl
);

public record InviteMemberRequest(
    [Required, EmailAddress] string Email,
    WorkspaceRole Role = WorkspaceRole.Developer
);

public record UpdateMemberRoleRequest(
    [Required] WorkspaceRole Role
);

public record WorkspaceDto(
    Guid Id,
    string Name,
    string? Description,
    string? LogoUrl,
    int MemberCount,
    DateTime CreatedAt
);

public record WorkspaceDetailDto(
    Guid Id,
    string Name,
    string? Description,
    string? LogoUrl,
    DateTime CreatedAt,
    IEnumerable<WorkspaceMemberDto> Members
);

public record WorkspaceMemberDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string? AvatarUrl,
    WorkspaceRole Role,
    string RoleName,
    DateTime JoinedAt
);