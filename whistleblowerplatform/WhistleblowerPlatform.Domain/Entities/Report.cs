using System;
using System.Collections.Generic;
using WhistleblowerPlatform.Domain.Enums;

namespace WhistleblowerPlatform.Domain.Entities;

public partial class Report : BaseEntity
{
    public Guid ReportId { get; set; }

    public string CaseNumber { get; set; } = null!;

    public byte[] TokenHash { get; set; } = null!;

    public int? CategoryId { get; set; }

    public ReportStatus Status { get; set; }

    public byte[] EncryptedContent { get; set; } = null!;

    public byte[] EncryptedKeyEnvelope { get; set; } = null!;

    public byte[] WbkeyEnvelope { get; set; } = null!;

    public byte[] WbpublicKey { get; set; } = null!;

    public bool SelfIdentified { get; set; }

    public byte[]? EncryptedIdentity { get; set; }

    public byte[]? EncryptedIdentityKeyEnvelope { get; set; }

    public byte[] EncryptedWbprivateKey { get; set; } = null!;

    public byte[] WbkeySalt { get; set; } = null!;

    public DateTime? AcknowledgedAt { get; set; }

    public DateTime AcknowledgementDueAt { get; set; }

    public DateTime FeedbackDueAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ReportCategory? Category { get; set; }

    public virtual ICollection<DeadlineTracking> DeadlineTrackings { get; set; } = new List<DeadlineTracking>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<ReportAttachment> ReportAttachments { get; set; } = new List<ReportAttachment>();
}
