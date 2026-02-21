using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Infrastructure;

public partial class NotificationQueue
{
    public Guid NotificationId { get; set; }

    public Guid RecipientId { get; set; }

    public byte NotificationType { get; set; }

    public Guid ReferenceId { get; set; }

    public string Subject { get; set; } = null!;

    public bool IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public int RetryCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Investigator Recipient { get; set; } = null!;
}
