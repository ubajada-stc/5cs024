using WhistleblowerPlatform.Domain.Entities;

namespace WhistleblowerPlatform.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(ApplicationUser user);
    string GenerateRefreshToken();
    Task StoreRefreshTokenAsync(ApplicationUser user, string rawToken);
    Task<bool> ValidateRefreshTokenAsync(ApplicationUser user, string rawToken);
    Task RevokeRefreshTokenAsync(ApplicationUser user);
}
