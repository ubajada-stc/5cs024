using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Infrastructure;

public partial class PlatformSetting
{
    public int SettingId { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Guid UpdatedBy { get; set; }

    public virtual Admin UpdatedByNavigation { get; set; } = null!;
}
