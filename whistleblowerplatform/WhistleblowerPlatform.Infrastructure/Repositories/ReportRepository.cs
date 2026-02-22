using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure.Persistence;
using WhistleblowerPlatform.Application.DTOs;

namespace WhistleblowerPlatform.Infrastructure.Repositories;

public class ReportRepository : IReportRepository
{
    private readonly WhistleblowerDbContext _context;

    public ReportRepository(WhistleblowerDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Report report)
    {
        _context.Reports.Add(report);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CategoryExistsAsync(int categoryId)
    {
        return await _context.ReportCategories
            .AnyAsync(c => c.CategoryId == categoryId && c.IsActive);
    }

    public async Task<string> GenerateCaseNumberAsync()
    {
        // Format: WB-YYYY-NNNN (e.g., WB-2026-0001)
        var year = DateTime.UtcNow.Year;
        var prefix = $"WB-{year}-";

        // Find the highest existing case number for this year
        var lastCaseNumber = await _context.Reports
            .Where(r => r.CaseNumber.StartsWith(prefix))
            .OrderByDescending(r => r.CaseNumber)
            .Select(r => r.CaseNumber)
            .FirstOrDefaultAsync();

        if (lastCaseNumber == null)
            return $"{prefix}0001";

        // Extract the sequence number and increment
        var sequencePart = lastCaseNumber.Substring(prefix.Length);
        var nextSequence = int.Parse(sequencePart) + 1;

        return $"{prefix}{nextSequence:D4}";
    }

    public async Task<List<ReportCategoryDto>> GetActiveCategoriesAsync()
    {
        return await _context.ReportCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .Select (c => new ReportCategoryDto
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
            })
            .ToListAsync();
    }
}
