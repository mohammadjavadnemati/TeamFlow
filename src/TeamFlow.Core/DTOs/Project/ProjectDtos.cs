using System.ComponentModel.DataAnnotations;
using TeamFlow.Core.Enums;

namespace TeamFlow.Core.DTOs.Project;

public record CreateProjectRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(1000)] string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    ProjectStatus Status = ProjectStatus.Planning,
    string Color = "#6366F1"
);

public record UpdateProjectRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(1000)] string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    ProjectStatus Status = ProjectStatus.Planning,
    string Color = "#6366F1"
);

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    ProjectStatus Status,
    string StatusName,
    string Color,
    Guid WorkspaceId,
    string CreatedByName,
    int SprintCount,
    double HealthScore,
    string HealthLabel,
    DateTime CreatedAt
);

public record ProjectDetailDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    ProjectStatus Status,
    string StatusName,
    string Color,
    Guid WorkspaceId,
    string CreatedByName,
    IEnumerable<SprintSummaryDto> Sprints,
    double HealthScore,
    string HealthLabel,
    DateTime CreatedAt
);

public record SprintSummaryDto(
    Guid Id,
    string Name,
    SprintStatus Status,
    string StatusName,
    DateTime StartDate,
    DateTime EndDate
);