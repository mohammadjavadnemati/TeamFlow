using System.ComponentModel.DataAnnotations;
using TeamFlow.Core.Enums;

namespace TeamFlow.Core.DTOs.Sprint;

public record CreateSprintRequest(
    [Required, StringLength(100)] string Name,
    [StringLength(500)] string? Goal,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate
);

public record UpdateSprintRequest(
    [StringLength(100)] string? Name,
    [StringLength(500)] string? Goal,
    [Required] DateTime StartDate,
    [Required] DateTime EndDate,
    SprintStatus Status = SprintStatus.Planned
);

public record SprintDto(
    Guid Id,
    string Name,
    string? Goal,
    DateTime StartDate,
    DateTime EndDate,
    SprintStatus Status,
    string StatusName,
    Guid ProjectId,
    string ProjectName,
    int DaysRemaining,
    DateTime CreatedAt
);