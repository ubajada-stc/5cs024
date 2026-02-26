using System;
using System.Collections.Generic;

namespace WhistleblowerPlatform.Domain.Entities;

public partial class ReportCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
}
