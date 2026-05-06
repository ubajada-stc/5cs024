using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly WhistleblowerDbContext _dbContext;

    public AdminRepository(WhistleblowerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Admin?> GetByIdAsync(Guid id)
    {
        return _dbContext.Admins.FirstOrDefaultAsync(a => a.AdminId == id);
    }

    public async Task UpdateLastLoginAsync(Guid id, DateTime lastLoginAt)
    {
        await _dbContext.Admins
            .Where(a => a.AdminId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.LastLoginAt, lastLoginAt));
    }

    public async Task SaveMfaAsync(Guid id, string secret, bool enabled)
    {
        await _dbContext.Admins
            .Where(a => a.AdminId == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Mfasecret, secret)
                .SetProperty(a => a.Mfaenabled, enabled));
    }
}
