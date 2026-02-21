using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Infrastructure;

public partial class Message
{
    public Guid MessageId { get; set; }

    public Guid ReportId { get; set; }

    public byte SenderRole { get; set; }

    public byte[] EncryptedContent { get; set; } = null!;

    public byte[] EncryptedKeyEnvelope { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Report Report { get; set; } = null!;
}
