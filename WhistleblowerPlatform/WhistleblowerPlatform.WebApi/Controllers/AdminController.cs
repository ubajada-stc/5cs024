using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UUIDNext;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly WhistleblowerDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(WhistleblowerDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ── US-4.1: Investigator Management ──────────────────────────────────────

    [HttpGet("investigators")]
    public async Task<IActionResult> GetInvestigators()
    {
        var investigators = await _context.Investigators
            .Select(i => new
            {
                i.InvestigatorId,
                i.Email,
                i.IsActive,
                i.CreatedAt,
                HasKeypair = i.PublicKey != null
            })
            .OrderBy(i => i.Email)
            .ToListAsync();

        return Ok(investigators);
    }

    [HttpPost("investigators")]
    public async Task<IActionResult> CreateInvestigator([FromBody] CreateInvestigatorRequest request)
    {
        if (await _context.Investigators.AnyAsync(i => i.Email == request.Email))
            return Conflict("An investigator with that email already exists.");

        var investigator = new Investigator
        {
            InvestigatorId = Uuid.NewSequential(),
            Email = request.Email,
            PasswordHash = string.Empty,
            Mfasecret = string.Empty,
            Mfaenabled = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Investigators.Add(investigator);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            InvestigatorId = investigator.InvestigatorId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _context.SaveChangesAsync();
        await WriteAuditLogAsync("CreateInvestigator", "Investigators", investigator.InvestigatorId.ToString());

        return Ok(new { investigator.InvestigatorId, investigator.Email });
    }

    [HttpPut("investigators/{id:guid}")]
    public async Task<IActionResult> UpdateInvestigator(Guid id, [FromBody] UpdateInvestigatorRequest request)
    {
        var investigator = await _context.Investigators.FindAsync(id);
        if (investigator is null) return NotFound();

        if (investigator.Email != request.Email)
        {
            if (await _context.Investigators.AnyAsync(i => i.Email == request.Email && i.InvestigatorId != id))
                return Conflict("Email already in use.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.InvestigatorId == id);
            if (user is not null)
            {
                user.UserName = request.Email;
                user.Email = request.Email;
                user.NormalizedEmail = request.Email.ToUpperInvariant();
                user.NormalizedUserName = request.Email.ToUpperInvariant();
                await _userManager.UpdateAsync(user);
            }

            investigator.Email = request.Email;
        }

        investigator.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await WriteAuditLogAsync("UpdateInvestigator", "Investigators", id.ToString());

        return NoContent();
    }

    [HttpPatch("investigators/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateInvestigator(Guid id)
    {
        var investigator = await _context.Investigators.FindAsync(id);
        if (investigator is null) return NotFound();

        investigator.IsActive = false;
        investigator.UpdatedAt = DateTime.UtcNow;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.InvestigatorId == id);
        if (user is not null)
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        await _context.SaveChangesAsync();
        await WriteAuditLogAsync("DeactivateInvestigator", "Investigators", id.ToString());

        return NoContent();
    }

    // ── US-4.2: Report Categories ─────────────────────────────────────────────

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.ReportCategories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new { c.CategoryId, c.Name, c.Description, c.DisplayOrder, c.IsActive })
            .ToListAsync();
        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] SaveCategoryRequest request)
    {
        var category = new ReportCategory
        {
            Name = request.Name,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };
        _context.ReportCategories.Add(category);
        await _context.SaveChangesAsync();
        await WriteAuditLogAsync("CreateCategory", "ReportCategories", category.CategoryId.ToString());
        return Ok(new { category.CategoryId });
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] SaveCategoryRequest request)
    {
        var category = await _context.ReportCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.Name = request.Name;
        category.Description = request.Description;
        category.DisplayOrder = request.DisplayOrder;
        await _context.SaveChangesAsync();
        await WriteAuditLogAsync("UpdateCategory", "ReportCategories", id.ToString());
        return NoContent();
    }

    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeactivateCategory(int id)
    {
        var category = await _context.ReportCategories.FindAsync(id);
        if (category is null) return NotFound();

        category.IsActive = false;
        await _context.SaveChangesAsync();
        await WriteAuditLogAsync("DeactivateCategory", "ReportCategories", id.ToString());
        return NoContent();
    }

    // ── US-4.3: Audit Logs ────────────────────────────────────────────────────

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] byte? actorType,
        [FromQuery] string? action,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (actorType.HasValue)
            query = query.Where(l => l.ActorType == actorType.Value);
        if (!string.IsNullOrEmpty(action))
            query = query.Where(l => l.Action.Contains(action));
        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.LogId,
                l.ActorType,
                l.ActorId,
                l.Action,
                l.TargetEntity,
                l.TargetId,
                l.Detail,
                l.Ipaddress,
                l.Timestamp
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, logs });
    }

    // ── US-4.4 + US-4.6: Platform Settings ───────────────────────────────────

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.PlatformSettings
            .OrderBy(s => s.SettingKey)
            .Select(s => new { s.SettingKey, s.SettingValue, s.Description, s.UpdatedAt })
            .ToListAsync();
        return Ok(settings);
    }

    [HttpPut("settings/{key}")]
    public async Task<IActionResult> UpdateSetting(string key, [FromBody] UpdateSettingRequest request)
    {
        var setting = await _context.PlatformSettings.FirstOrDefaultAsync(s => s.SettingKey == key);
        if (setting is null) return NotFound();

        var adminUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Id.ToString() == User.FindFirstValue(ClaimTypes.NameIdentifier));
        if (adminUser?.AdminId is null) return Forbid();

        setting.SettingValue = request.Value;
        setting.UpdatedAt = DateTime.UtcNow;
        setting.UpdatedBy = adminUser.AdminId.Value;

        await _context.SaveChangesAsync();
        await WriteAuditLogAsync($"UpdateSetting:{key}", "PlatformSettings", key);

        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task WriteAuditLogAsync(string action, string targetEntity, string targetId)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _context.AuditLogs.Add(new AuditLog
        {
            ActorType = 2,
            ActorId = adminId,
            Action = action,
            TargetEntity = targetEntity,
            TargetId = targetId,
            Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}

public class CreateInvestigatorRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UpdateInvestigatorRequest
{
    public string Email { get; set; } = string.Empty;
}

public class SaveCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
}

public class UpdateSettingRequest
{
    public string Value { get; set; } = string.Empty;
}
