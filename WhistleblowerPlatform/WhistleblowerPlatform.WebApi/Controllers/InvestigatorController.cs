using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Application.Validators;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Domain.Enums;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/investigator")]
[Authorize]
public class InvestigatorController : ControllerBase
{
    private readonly WhistleblowerDbContext _dbContext;
    private readonly IAttachmentStorageService _storageService;

    public InvestigatorController(WhistleblowerDbContext dbContext, IAttachmentStorageService storageService)
    {
        _dbContext = dbContext;
        _storageService = storageService;
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
            iv = investigator.PrivateKeyIv,
            publicKey = investigator.PublicKey
        });
    }

    [HttpGet("cases")]
    public async Task<IActionResult> GetCases()
    {
        var investigator = await GetCurrentInvestigatorAsync();
        if (investigator is null) return Unauthorized();

        var reports = await _dbContext.Reports
            .Where(r => !r.IsDeleted)
            .Include(r => r.Category)
            .Include(r => r.ReportAttachments)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var result = reports.Select(r => new CaseListItemDto
        {
            CaseNumber = r.CaseNumber,
            Status = (byte)r.Status,
            CategoryName = r.Category?.Name,
            CreatedAt = r.CreatedAt,
            AcknowledgementDueAt = r.AcknowledgementDueAt,
            FeedbackDueAt = r.FeedbackDueAt,
            IsAcknowledged = r.AcknowledgedAt.HasValue,
            AttachmentCount = r.ReportAttachments.Count
        }).ToList();

        return Ok(result);
    }

    [HttpGet("cases/{caseNumber}")]
    public async Task<IActionResult> GetCaseDetail(string caseNumber)
    {
        var investigator = await GetCurrentInvestigatorAsync();
        if (investigator is null) return Unauthorized();

        var report = await _dbContext.Reports
            .Where(r => r.CaseNumber == caseNumber && !r.IsDeleted)
            .Include(r => r.Category)
            .Include(r => r.ReportAttachments)
            .Include(r => r.Messages)
            .FirstOrDefaultAsync();

        if (report is null) return NotFound(new { error = "Case not found." });

        var attachments = report.ReportAttachments.Select(a => new AttachmentInfoDto
        {
            AttachmentId = a.AttachmentId,
            FileName = DecodeFileName(a.EncryptedFileName),
            MimeType = a.MimeType,
            FileSize = a.FileSize,
            SanitizationStatus = (byte)a.SanitizationStatus,
            SanitizationError = a.SanitizationError,
            EncryptedKeyEnvelope = a.EncryptedKeyEnvelope
        }).ToList();

        var detail = new CaseDetailDto
        {
            CaseNumber = report.CaseNumber,
            Status = (byte)report.Status,
            CategoryName = report.Category?.Name,
            CreatedAt = report.CreatedAt,
            AcknowledgementDueAt = report.AcknowledgementDueAt,
            FeedbackDueAt = report.FeedbackDueAt,
            IsAcknowledged = report.AcknowledgedAt.HasValue,
            SelfIdentified = report.SelfIdentified,
            EncryptedContent = report.EncryptedContent,
            EncryptedKeyEnvelope = report.EncryptedKeyEnvelope,
            EncryptedIdentity = report.EncryptedIdentity,
            EncryptedIdentityKeyEnvelope = report.EncryptedIdentityKeyEnvelope,
            WbPublicKey = report.WbpublicKey ?? [],
            Attachments = attachments,
            Messages = report.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    SenderRole = m.SenderRole,
                    EncryptedContent = m.EncryptedContent,
                    EncryptedKeyEnvelope = m.EncryptedKeyEnvelope,
                    CreatedAt = m.CreatedAt
                }).ToList()
        };

        return Ok(detail);
    }

    [HttpGet("cases/{caseNumber}/attachments/{attachmentId:guid}")]
    public async Task<IActionResult> GetAttachment(string caseNumber, Guid attachmentId)
    {
        var investigator = await GetCurrentInvestigatorAsync();
        if (investigator is null) return Unauthorized();

        var report = await _dbContext.Reports
            .Where(r => r.CaseNumber == caseNumber && !r.IsDeleted)
            .Select(r => new { r.ReportId })
            .FirstOrDefaultAsync();

        if (report is null) return NotFound(new { error = "Case not found." });

        var attachment = await _dbContext.ReportAttachments
            .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId && a.ReportId == report.ReportId);

        if (attachment is null) return NotFound(new { error = "Attachment not found." });

        var blobBytes = await _storageService.ReadAsync(attachment.StoragePath);

        string iv, ciphertext, authTag;

        if (attachment.SanitizationStatus == SanitizationStatus.Completed)
        {
            // Server-produced format: IV (12 bytes) || Ciphertext || AuthTag (16 bytes)
            iv = Convert.ToBase64String(blobBytes[..12]);
            authTag = Convert.ToBase64String(blobBytes[^16..]);
            ciphertext = Convert.ToBase64String(blobBytes[12..^16]);
        }
        else
        {
            // Client-produced format: UTF8(JSON({Iv, Ciphertext, AuthTag}))
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
            encryptedKeyEnvelope = Convert.ToBase64String(attachment.EncryptedKeyEnvelope),
            mimeType = attachment.MimeType,
            fileName = DecodeFileName(attachment.EncryptedFileName),
            sanitizationStatus = (byte)attachment.SanitizationStatus
        });
    }

    [HttpPost("cases/{caseNumber}/messages")]
    public async Task<IActionResult> SendMessage(string caseNumber, [FromBody] SendMessageRequest request)
    {
        var investigator = await GetCurrentInvestigatorAsync();
        if (investigator is null) return Unauthorized();

        var report = await _dbContext.Reports
            .Where(r => r.CaseNumber == caseNumber && !r.IsDeleted)
            .FirstOrDefaultAsync();
        if (report is null) return NotFound(new { error = "Case not found." });

        var message = new Message
        {
            MessageId = Guid.NewGuid(),
            ReportId = report.ReportId,
            SenderRole = 0,
            EncryptedContent = request.EncryptedContent,
            EncryptedKeyEnvelope = request.EncryptedKeyEnvelope,
            WbkeyEnvelope = request.WbKeyEnvelope,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Messages.Add(message);
        await _dbContext.SaveChangesAsync();

        return Ok(new { messageId = message.MessageId, createdAt = message.CreatedAt });
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

    private async Task<Investigator?> GetCurrentInvestigatorAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return null;

        var appUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (appUser?.InvestigatorId is null) return null;

        return await _dbContext.Investigators
            .FirstOrDefaultAsync(i => i.InvestigatorId == appUser.InvestigatorId.Value);
    }

    private record EncryptedBlobParts(string? Iv, string? Ciphertext, string? AuthTag);
}
