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
                r.ReportId,
                r.EncryptedWbprivateKey,
                r.WbpublicKey,
                r.EncryptedContent,
                ReportWbKeyEnvelope = r.WbkeyEnvelope,
                Messages = r.Messages
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

        await _dbContext.Messages
            .Where(m => m.ReportId == data.ReportId && m.SenderRole == 0 && !m.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.IsRead, true));

        return Ok(new
        {
            encryptedWbPrivateKey = Convert.ToBase64String(data.EncryptedWbprivateKey),
            wbPublicKey = Convert.ToBase64String(data.WbpublicKey),
            reportEncryptedContent = data.EncryptedContent,
            reportWbKeyEnvelope = data.ReportWbKeyEnvelope,
            messages = data.Messages
        });
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendWbMessageRequest request)
    {
        var tokenHashBase64 = Request.Headers["X-WB-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tokenHashBase64))
            return Unauthorized(new { error = "Missing token." });

        byte[] tokenHash;
        try { tokenHash = Convert.FromBase64String(tokenHashBase64); }
        catch { return Unauthorized(new { error = "Invalid token format." }); }

        var report = await _dbContext.Reports
            .Where(r => r.TokenHash == tokenHash && !r.IsDeleted)
            .FirstOrDefaultAsync();

        if (report is null)
            return Unauthorized(new { error = "Invalid token." });

        var message = new WhistleblowerPlatform.Domain.Entities.Message
        {
            MessageId = Guid.NewGuid(),
            ReportId = report.ReportId,
            SenderRole = 1,
            EncryptedContent = request.EncryptedContent,
            EncryptedKeyEnvelope = request.EncryptedKeyEnvelope,
            WbkeyEnvelope = request.WbKeyEnvelope,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);

        _dbContext.AuditLogs.Add(new WhistleblowerPlatform.Domain.Entities.AuditLog
        {
            ActorType = 0,
            Action = "WbMessageSent",
            TargetEntity = "Messages",
            TargetId = report.CaseNumber,
            Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();

        return Ok(new { messageId = message.MessageId, createdAt = message.CreatedAt });
    }
}

public class SendWbMessageRequest
{
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
    public byte[]? WbKeyEnvelope { get; set; }
}
