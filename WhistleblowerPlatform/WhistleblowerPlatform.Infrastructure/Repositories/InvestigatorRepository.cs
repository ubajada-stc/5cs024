using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.Infrastructure.Repositories;

public class InvestigatorRepository : IInvestigatorRepository
{
    private readonly WhistleblowerDbContext _dbContext;

    public InvestigatorRepository(WhistleblowerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Investigator?> GetByIdAsync(Guid id)
    {
        return _dbContext.Investigators.FirstOrDefaultAsync(i => i.InvestigatorId == id);
    }

    public async Task UpdateLastLoginAsync(Guid id, DateTime lastLoginAt)
    {
        await _dbContext.Investigators
            .Where(i => i.InvestigatorId == id)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.LastLoginAt, lastLoginAt));
    }
}
