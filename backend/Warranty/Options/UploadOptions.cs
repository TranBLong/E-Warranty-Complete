using System.ComponentModel.DataAnnotations;

namespace Warranty.Options;

/// <summary>
/// File upload configuration (Options Pattern – Buổi 10).
/// Bind from section "Upload".
/// </summary>
public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>Root folder for uploaded files (relative to content root or absolute).</summary>
    [Required]
    public string RootPath { get; set; } = "App_Data/uploads";

    /// <summary>Max file size in bytes (default 10 MB).</summary>
    [Range(1024, 104_857_600)]
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    /// <summary>Allowed content types (MIME).</summary>
    public string[] AllowedContentTypes { get; set; } =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "application/pdf"
    ];

    /// <summary>Allowed file extensions (lowercase, with dot).</summary>
    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf"
    ];
}
