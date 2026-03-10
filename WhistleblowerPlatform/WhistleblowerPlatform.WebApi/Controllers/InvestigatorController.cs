using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Validators;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/investigator")]
[Authorize]
public class InvestigatorController : ControllerBase
{
    private readonly WhistleblowerDbContext _dbContext;

    public InvestigatorController(WhistleblowerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("keypair-status")]
    public async Task<IActionResult> GetKeypairStatus()
    {
        var investigator = await GetCurrentInvestigatorAsync();
        if (investigator is null) return Unauthorized();

        return Ok(new
        {
            hasKeypair = investigator.PublicKey != null && investigator.PublicKey.Length > 0
                      && investigator.PrivateKeySalt != null,
            email = investigator.Email
        });
    }

    [HttpPost("setup-keypair")]
    public async Task<IActionResult> SetupKeypair([FromBody] SetupKeypairRequest request)
    {
        var errors = SetupKeypairValidator.Validate(request);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var investigator = await GetCurrentInvestigatorAsync();
        if (investigator is null) return Unauthorized();

        if (investigator.PublicKey != null && investigator.PublicKey.Length > 0
            && investigator.PrivateKeySalt != null)
            return BadRequest(new { error = "Keypair already exists for this investigator." });

        investigator.PublicKey = request.PublicKey;
        investigator.EncryptedPrivateKey = request.EncryptedPrivateKey;
        investigator.PrivateKeySalt = request.PrivateKeySalt;
        investigator.PrivateKeyIv = request.PrivateKeyIv;
        investigator.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpGet("encrypted-key")]
    public async Task<IActionResult> GetEncryptedKey()
    {
        var investigator = await GetCurrentInvestigatorAsync();
        if (investigator is null) return Unauthorized();

        if (investigator.EncryptedPrivateKey is null || investigator.EncryptedPrivateKey.Length == 0)
            return NotFound(new { error = "No keypair found for this investigator." });

        return Ok(new
        {
            encryptedPrivateKey = investigator.EncryptedPrivateKey,
            salt = investigator.PrivateKeySalt,
            iv = investigator.PrivateKeyIv
        });
    }

    private async Task<Investigator?> GetCurrentInvestigatorAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return null;

        var appUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (appUser?.InvestigatorId is null) return null;

        return await _dbContext.Investigators
            .FirstOrDefaultAsync(i => i.InvestigatorId == appUser.InvestigatorId.Value);
    }
}
