using WhistleblowerPlatform.Domain.Entities;

namespace WhistleblowerPlatform.Application.Interfaces;

public interface IInvestigatorRepository
{
    Task<Investigator?> GetByIdAsync(Guid id);
    Task UpdateLastLoginAsync(Guid id, DateTime lastLoginAt);
}
