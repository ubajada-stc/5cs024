namespace WhistleblowerPlatform.Application.DTOs;

public class CaseDetailDto
{
    public string CaseNumber { get; set; } = "";
    public byte Status { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime AcknowledgementDueAt { get; set; }
    public DateTime FeedbackDueAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool SelfIdentified { get; set; }
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
    public byte[]? EncryptedIdentity { get; set; }
    public byte[]? EncryptedIdentityKeyEnvelope { get; set; }
    public List<AttachmentInfoDto> Attachments { get; set; } = [];
}

public class AttachmentInfoDto
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public byte SanitizationStatus { get; set; }
    public string? SanitizationError { get; set; }
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
}
