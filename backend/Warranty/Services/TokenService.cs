using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Warranty.Models;
using Warranty.Options;

namespace Warranty.Services;

/// <summary>
/// Issues JWT access tokens using strongly-typed <see cref="JwtOptions"/> (Options Pattern).
/// </summary>
public sealed class TokenService
{
    private readonly JwtOptions _options;
    private readonly byte[] _signingKey;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.SigningKey) ||
            Encoding.UTF8.GetByteCount(_options.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured with at least 32 UTF-8 bytes. " +
                "Use User Secrets (Development) or environment variables (Production).");
        }

        _signingKey = Encoding.UTF8.GetBytes(_options.SigningKey);
    }

    public (string Token, DateTime ExpiresAt) CreateAccessToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(_signingKey),
            SecurityAlgorithms.HmacSha256);
        var role = user.Role.ToString();

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("role", role),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            ],
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public int RefreshTokenDays => _options.RefreshTokenDays;
}
