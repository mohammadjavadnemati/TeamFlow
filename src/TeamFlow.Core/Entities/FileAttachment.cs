namespace TeamFlow.Core.Entities;

public class FileAttachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;

    public Guid TaskId { get; set; }
    public ProjectTask Task { get; set; } = null!;

    public Guid UploadedById { get; set; }
    public ApplicationUser UploadedBy { get; set; } = null!;
}