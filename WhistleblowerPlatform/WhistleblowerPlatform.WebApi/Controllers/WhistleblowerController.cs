using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/whistleblower")]
public class WhistleblowerController : ControllerBase
{
    private readonly WhistleblowerDbContext _dbContext;

    public WhistleblowerController(WhistleblowerDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("case")]
    public async Task<IActionResult> GetCase()
    {
        var tokenHashBase64 = Request.Headers["X-WB-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tokenHashBase64))
            return Unauthorized(new { error = "Missing token." });

        byte[] tokenHash;
        try { tokenHash = Convert.FromBase64String(tokenHashBase64); }
        catch { return Unauthorized(new { error = "Invalid token format." }); }

        var report = await _dbContext.Reports
            .Where(r => r.TokenHash == tokenHash)
            .Select(r => new { r.CaseNumber, r.Status, r.IsDeleted })
            .FirstOrDefaultAsync();

        if (report is null)
            return Unauthorized(new { error = "Invalid token." });

        if (report.IsDeleted)
            return StatusCode(410, new { error = "This case has been permanently deleted." });

        return Ok(new
        {
            caseNumber = report.CaseNumber,
            status = (byte)report.Status
        });
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetMessages()
    {
        var tokenHashBase64 = Request.Headers["X-WB-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tokenHashBase64))
            return Unauthorized(new { error = "Missing token." });

        byte[] tokenHash;
        try { tokenHash = Convert.FromBase64String(tokenHashBase64); }
        catch { return Unauthorized(new { error = "Invalid token format." }); }

        var data = await _dbContext.Reports
            .Where(r => r.TokenHash == tokenHash && !r.IsDeleted)
            .Select(r => new
            {
                r.EncryptedWbprivateKey,
                r.EncryptedContent,
                ReportWbKeyEnvelope = r.WbkeyEnvelope,
                Messages = r.Messages
                    .Where(m => m.WbkeyEnvelope != null)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new
                    {
                        m.MessageId,
                        m.SenderRole,
                        m.EncryptedContent,
                        WbKeyEnvelope = m.WbkeyEnvelope,
                        m.CreatedAt
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (data is null)
            return Unauthorized(new { error = "Invalid token." });

        return Ok(new
        {
            encryptedWbPrivateKey = Convert.ToBase64String(data.EncryptedWbprivateKey),
            reportEncryptedContent = data.EncryptedContent,
            reportWbKeyEnvelope = data.ReportWbKeyEnvelope,
            messages = data.Messages
        });
    }
}
