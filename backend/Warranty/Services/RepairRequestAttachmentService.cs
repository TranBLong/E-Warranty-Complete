using Microsoft.Extensions.Options;
using Warranty.Data;
using Warranty.Models;
using Warranty.Options;

namespace Warranty.Services;

/// <summary>
/// Handles file upload for repair request attachments using <see cref="UploadOptions"/>.
/// </summary>
public sealed class RepairRequestAttachmentService(
    AppDbContext dbContext,
    IOptions<UploadOptions> uploadOptions,
    IWebHostEnvironment env,
    ILogger<RepairRequestAttachmentService> logger)
{
    private readonly UploadOptions _options = uploadOptions.Value;

    public async Task<RepairRequestAttachment> SaveAsync(
        int repairRequestId,
        int uploadedById,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(env.ContentRootPath, _options.RootPath);

        var folder = Path.Combine(root, "repair-requests", repairRequestId.ToString());
        Directory.CreateDirectory(folder);

        var safeName = SanitizeFileName(file.FileName);
        var uniqueName = $"{Guid.NewGuid():N}_{safeName}";
        var fullPath = Path.Combine(folder, uniqueName);
        var relativePath = Path.Combine("repair-requests", repairRequestId.ToString(), uniqueName)
            .Replace('\\', '/');

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var attachment = new RepairRequestAttachment
        {
            RepairRequestId = repairRequestId,
            UploadedById = uploadedById,
            FileName = safeName,
            FilePath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        dbContext.RepairRequestAttachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Saved attachment {FileName} ({Size} bytes) for RepairRequest {Id}",
            safeName, file.Length, repairRequestId);

        return attachment;
    }

    public string GetAbsolutePath(string relativePath)
    {
        var root = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(env.ContentRootPath, _options.RootPath);
        return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private void ValidateFile(IFormFile file)
    {
        if (file.Length <= 0)
            throw new InvalidOperationException("Empty file is not allowed.");

        if (file.Length > _options.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"File exceeds maximum size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"File extension '{ext}' is not allowed.");

        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Content type '{file.ContentType}' is not allowed.");
    }

    private static string SanitizeFileName(string name)
    {
        var fileName = Path.GetFileName(name);
        foreach (var c in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(c, '_');
        return string.IsNullOrWhiteSpace(fileName) ? "file" : fileName;
    }
}
