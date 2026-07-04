namespace TeamFlow.Core.Entities;

public class ChecklistItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public bool IsChecked { get; set; } = false;
    public int Order { get; set; } = 0;

    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;
}