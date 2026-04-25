using System;
using System.Collections.Generic;
using WhistleblowerPlatform.Domain.Enums;

namespace WhistleblowerPlatform.Domain.Entities;

//Not inheriting from BaseEntity class since report attachment only uses createdat
public partial class ReportAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid ReportId { get; set; }

    public string StoragePath { get; set; } = null!;

    public string? SanitizationStoragePath { get; set; }

    public byte[]? SanitizationKey { get; set; }

    public SanitizationStatus SanitizationStatus { get; set; } = SanitizationStatus.Queued;

    public string? SanitizationError { get; set; }

    public DateTime? SanitizedAt { get; set; }

    public string? OriginalStoragePath { get; set; }

    public byte[]? OriginalPlaintextHash { get; set; }

    public byte[] EncryptedKeyEnvelope { get; set; } = null!;

    public byte[] WbkeyEnvelope { get; set; } = null!;

    public byte[] EncryptedFileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Report Report { get; set; } = null!;
}
