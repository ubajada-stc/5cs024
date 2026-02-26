using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Domain.Entities;

public partial class DeadlineTracking
{
    public Guid TrackingId { get; set; }

    public Guid ReportId { get; set; }

    public byte DeadlineType { get; set; }

    public DateTime DueAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public bool IsOverdue { get; set; }

    public DateTime? NotifiedAt { get; set; }

    public virtual Report Report { get; set; } = null!;
}
