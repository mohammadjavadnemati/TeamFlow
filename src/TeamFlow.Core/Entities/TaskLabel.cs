namespace TeamFlow.Core.Entities;

public class TaskLabel
{
    public Guid TaskId { get; set; }
    public ProjectTask Task { get; set; } = null!;

    public Guid LabelId { get; set; }
    public Label Label { get; set; } = null!;
}