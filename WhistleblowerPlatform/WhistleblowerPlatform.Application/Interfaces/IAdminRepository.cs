using WhistleblowerPlatform.Domain.Entities;

namespace WhistleblowerPlatform.Application.Interfaces;

public interface IAdminRepository
{
    Task<Admin?> GetByIdAsync(Guid id);
    Task UpdateLastLoginAsync(Guid id, DateTime lastLoginAt);
    Task SaveMfaAsync(Guid id, string secret, bool enabled);
}
