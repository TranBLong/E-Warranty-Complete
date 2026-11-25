using System.ComponentModel.DataAnnotations;

namespace Warranty.Options;

/// <summary>
/// Strongly-typed JWT configuration (Options Pattern – Buổi 10).
/// Bind from section "Jwt". SigningKey should come from User Secrets / env in production.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "Warranty.Api";

    [Required]
    public string Audience { get; set; } = "Warranty.Client";

    /// <summary>Minimum 32 UTF-8 bytes. Prefer User Secrets or environment variable.</summary>
    [Required]
    [MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes (default 30).</summary>
    [Range(1, 1440)]
    public int AccessTokenMinutes { get; set; } = 30;

    /// <summary>Refresh token lifetime in days (default 7).</summary>
    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
