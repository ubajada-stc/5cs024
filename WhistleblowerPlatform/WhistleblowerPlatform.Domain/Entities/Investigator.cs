using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Domain.Entities;

public partial class Investigator
{
    public Guid InvestigatorId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Mfasecret { get; set; } = null!;

    public bool Mfaenabled { get; set; }

    public byte[] PublicKey { get; set; } = null!;

    public byte[] EncryptedPrivateKey { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<NotificationQueue> NotificationQueues { get; set; } = new List<NotificationQueue>();
}
