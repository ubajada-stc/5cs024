using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Entities;

namespace WhistleblowerPlatform.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<ApplicationUser> _userManager;

    public TokenService(JwtSettings jwtSettings, UserManager<ApplicationUser> userManager)
    {
        _jwtSettings = jwtSettings;
        _userManager = userManager;
    }

    public string GenerateAccessToken(ApplicationUser user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    public async Task StoreRefreshTokenAsync(ApplicationUser user, string rawToken)
    {
        var hash = HashToken(rawToken);
        var expiry = DateTime.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiryHours).Ticks;
        await _userManager.SetAuthenticationTokenAsync(
            user, "WhistleblowerPlatform", "RefreshToken", $"{hash}:{expiry}");
    }

    public async Task<bool> ValidateRefreshTokenAsync(ApplicationUser user, string rawToken)
    {
        var stored = await _userManager.GetAuthenticationTokenAsync(
            user, "WhistleblowerPlatform", "RefreshToken");

        if (stored is null) return false;

        var parts = stored.Split(':');
        if (parts.Length != 2) return false;

        var storedHash = parts[0];
        if (!long.TryParse(parts[1], out var expiryTicks)) return false;
        if (new DateTime(expiryTicks, DateTimeKind.Utc) < DateTime.UtcNow) return false;

        return storedHash == HashToken(rawToken);
    }

    public async Task RevokeRefreshTokenAsync(ApplicationUser user)
    {
        await _userManager.RemoveAuthenticationTokenAsync(
            user, "WhistleblowerPlatform", "RefreshToken");
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }
}
