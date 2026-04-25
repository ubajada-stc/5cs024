using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using WhistleblowerPlatform.Domain.Enums;
using WhistleblowerPlatform.Infrastructure.Persistence;
using WhistleblowerPlatform.Application.Interfaces;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/whistleblower")]
public class WhistleblowerController : ControllerBase
{
    private readonly WhistleblowerDbContext _dbContext;
    private readonly IAttachmentStorageService _storageService;

    public WhistleblowerController(WhistleblowerDbContext dbContext, IAttachmentStorageService storageService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
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
                    .ToList(),
                Attachments = r.ReportAttachments
                    .Select(a => new
                    {
                        a.AttachmentId,
                        a.EncryptedFileName,
                        a.MimeType,
                        a.FileSize,
                        SanitizationStatus = (byte)a.SanitizationStatus,
                        WbKeyEnvelope = a.WbkeyEnvelope
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
            messages = data.Messages,
            attachments = data.Attachments.Select(a => new
            {
                a.AttachmentId,
                fileName = DecodeFileName(a.EncryptedFileName),
                a.MimeType,
                a.FileSize,
                a.SanitizationStatus,
                wbKeyEnvelope = a.WbKeyEnvelope
            })
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

    [HttpGet("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> GetAttachment(Guid attachmentId)
    {
        var tokenHashBase64 = Request.Headers["X-WB-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(tokenHashBase64))
            return Unauthorized(new { error = "Missing token." });

        byte[] tokenHash;
        try { tokenHash = Convert.FromBase64String(tokenHashBase64); }
        catch { return Unauthorized(new { error = "Invalid token format." }); }

        var report = await _dbContext.Reports
            .Where(r => r.TokenHash == tokenHash && !r.IsDeleted)
            .Select(r => new { r.ReportId })
            .FirstOrDefaultAsync();

        if (report is null)
            return Unauthorized(new { error = "Invalid token." });

        var attachment = await _dbContext.ReportAttachments
            .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId && a.ReportId == report.ReportId);

        if (attachment is null) return NotFound(new { error = "Attachment not found." });

        var blobBytes = await _storageService.ReadAsync(attachment.StoragePath);

        string iv, ciphertext, authTag;

        if (attachment.SanitizationStatus == SanitizationStatus.Completed)
        {
            iv = Convert.ToBase64String(blobBytes[..12]);
            authTag = Convert.ToBase64String(blobBytes[^16..]);
            ciphertext = Convert.ToBase64String(blobBytes[12..^16]);
        }
        else
        {
            var json = Encoding.UTF8.GetString(blobBytes);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parts = JsonSerializer.Deserialize<EncryptedBlobParts>(json, options);
            if (parts?.Iv is null || parts.Ciphertext is null || parts.AuthTag is null)
                return BadRequest(new { error = "Invalid encrypted blob format." });
            iv = parts.Iv;
            ciphertext = parts.Ciphertext;
            authTag = parts.AuthTag;
        }

        return Ok(new
        {
            iv,
            ciphertext,
            authTag,
            wbKeyEnvelope = Convert.ToBase64String(attachment.WbkeyEnvelope),
            mimeType = attachment.MimeType,
            fileName = DecodeFileName(attachment.EncryptedFileName),
            sanitizationStatus = (byte)attachment.SanitizationStatus
        });
    }

    private static string DecodeFileName(byte[] encryptedFileName)
    {
        try
        {
            var b64 = Encoding.UTF8.GetString(encryptedFileName);
            return Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }
        catch
        {
            return "unknown";
        }
    }

    private record EncryptedBlobParts(string? Iv, string? Ciphertext, string? AuthTag);
}

public class SendWbMessageRequest
{
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
    public byte[]? WbKeyEnvelope { get; set; }
}
