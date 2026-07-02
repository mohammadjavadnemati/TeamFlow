using TeamFlow.Core.Enums;

namespace TeamFlow.Core.Entities;

public class Sprint : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public SprintStatus Status { get; set; } = SprintStatus.Planned;

    // FK
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}