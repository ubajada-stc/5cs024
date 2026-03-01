using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Domain.Enums;

public enum SanitizationStatus : byte
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4
}
