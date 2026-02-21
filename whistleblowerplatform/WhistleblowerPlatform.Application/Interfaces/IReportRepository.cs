using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhistleblowerPlatform.Domain.Entities;

namespace WhistleblowerPlatform.Application.Interfaces
{
    public interface IReportRepository
    {
        Task AddAsync(Report report);
        Task<bool> CategoryExistsAsync(int categoryId);
        Task<string> GenerateCaseNumberAsync();
    }
}
