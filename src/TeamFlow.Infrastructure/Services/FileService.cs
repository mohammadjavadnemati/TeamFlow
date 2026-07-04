using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TeamFlow.Core.DTOs.File;
using TeamFlow.Core.Entities;
using TeamFlow.Core.Enums;
using TeamFlow.Core.Interfaces;
using TeamFlow.Infrastructure.Data;

namespace TeamFlow.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly ApplicationDbContext _context;
    private readonly IWorkspaceService _workspaceService;
    private readonly IActivityService _activityService;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    private static readonly string[] AllowedTypes =
    [
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/zip", "application/x-zip-compressed",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain"
    ];

    private const long MaxFileSize = 20 * 1024 * 1024; // 20MB

    public FileService(
        ApplicationDbContext context,
        IWorkspaceService workspaceService,
        IActivityService activityService,
        IConfiguration configuration)
    {
        _context = context;
        _workspaceService = workspaceService;
        _activityService = activityService;
        _blobServiceClient = new BlobServiceClient(configuration["AzureStorage:ConnectionString"]);
        _containerName = configuration["AzureStorage:ContainerName"] ?? "teamflow-files";
    }

    public async Task<FileAttachmentDto> UploadAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId, IFormFile file)
    {
        await EnsureMemberAsync(workspaceId, userId);

        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId)
            ?? throw new KeyNotFoundException("Task یافت نشد.");

        // Validation
        if (file.Length == 0)
            throw new InvalidOperationException("فایل خالی است.");
        if (file.Length > MaxFileSize)
            throw new InvalidOperationException("حجم فایل نباید بیشتر از ۲۰ مگابایت باشد.");
        if (!AllowedTypes.Contains(file.ContentType.ToLower()))
            throw new InvalidOperationException("نوع فایل مجاز نیست.");

        // Upload to Azure Blob
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobName = $"{workspaceId}/{projectId}/{taskId}/{Guid.NewGuid()}_{file.FileName}";
        var blobClient = containerClient.GetBlobClient(blobName);

        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

        var attachment = new FileAttachment
        {
            FileName = blobName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            BlobUrl = blobClient.Uri.ToString(),
            ContainerName = _containerName,
            TaskId = taskId,
            UploadedById = userId
        };

        _context.FileAttachments.Add(attachment);
        await _context.SaveChangesAsync();

        await _context.Entry(attachment).Reference(a => a.UploadedBy).LoadAsync();

        await _activityService.LogAsync(workspaceId, userId, ActivityType.FileUploaded,
            $"فایل '{file.FileName}' آپلود شد.", projectId, taskId);

        return MapToDto(attachment);
    }

    public async Task<IEnumerable<FileAttachmentDto>> GetAllAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid userId)
    {
        await EnsureMemberAsync(workspaceId, userId);

        return await _context.FileAttachments
            .Where(f => f.TaskId == taskId)
            .Include(f => f.UploadedBy)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => MapToDto(f))
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid workspaceId, Guid projectId, Guid taskId, Guid fileId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");

        var attachment = await _context.FileAttachments
            .FirstOrDefaultAsync(f => f.Id == fileId && f.TaskId == taskId)
            ?? throw new KeyNotFoundException("فایل یافت نشد.");

        if (attachment.UploadedById != userId && (int)role > (int)WorkspaceRole.Admin)
            throw new UnauthorizedAccessException("دسترسی کافی ندارید.");

        // Delete from Azure Blob
        var containerClient = _blobServiceClient.GetBlobContainerClient(attachment.ContainerName);
        var blobClient = containerClient.GetBlobClient(attachment.FileName);
        await blobClient.DeleteIfExistsAsync();

        _context.FileAttachments.Remove(attachment);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureMemberAsync(Guid workspaceId, Guid userId)
    {
        var role = await _workspaceService.GetUserRoleAsync(workspaceId, userId);
        if (role is null)
            throw new UnauthorizedAccessException("شما عضو این Workspace نیستید.");
    }

    private static FileAttachmentDto MapToDto(FileAttachment f) => new(
        f.Id,
        f.OriginalFileName,
        f.ContentType,
        f.FileSize,
        FormatFileSize(f.FileSize),
        f.BlobUrl,
        $"{f.UploadedBy.FirstName} {f.UploadedBy.LastName}",
        f.CreatedAt
    );

    private static string FormatFileSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024):F1} MB"
    };
}