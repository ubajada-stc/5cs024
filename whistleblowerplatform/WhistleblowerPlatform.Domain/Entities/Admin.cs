using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Infrastructure;

public partial class Admin
{
    public Guid AdminId { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Mfasecret { get; set; } = null!;

    public bool Mfaenabled { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<PlatformSetting> PlatformSettings { get; set; } = new List<PlatformSetting>();
}
