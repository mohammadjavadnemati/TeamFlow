namespace TeamFlow.Core.DTOs.File;

public record FileAttachmentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string FileSizeFormatted,
    string BlobUrl,
    string UploadedByName,
    DateTime CreatedAt
);