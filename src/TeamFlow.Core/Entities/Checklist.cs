namespace TeamFlow.Core.Entities;

public class Checklist : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public Guid TaskId { get; set; }
    public ProjectTask Task { get; set; } = null!;

    public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}