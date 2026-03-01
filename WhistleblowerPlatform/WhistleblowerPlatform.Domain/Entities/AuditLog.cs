using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Domain.Entities;

public partial class AuditLog
{
    public long LogId { get; set; }

    public byte ActorType { get; set; }

    public string? ActorId { get; set; }

    public string Action { get; set; } = null!;

    public string? TargetEntity { get; set; }

    public string? TargetId { get; set; }

    public string? Detail { get; set; }

    public string? Ipaddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime Timestamp { get; set; }
}
