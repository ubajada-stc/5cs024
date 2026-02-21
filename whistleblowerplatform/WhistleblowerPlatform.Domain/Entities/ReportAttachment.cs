using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Infrastructure;

public partial class ReportAttachment
{
    public Guid AttachmentId { get; set; }

    public Guid ReportId { get; set; }

    public string StoragePath { get; set; } = null!;

    public byte[] EncryptedKeyEnvelope { get; set; } = null!;

    public byte[] WbkeyEnvelope { get; set; } = null!;

    public byte[] EncryptedFileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Report Report { get; set; } = null!;
}
