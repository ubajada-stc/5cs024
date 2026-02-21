using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Domain.Enums;

public enum ReportStatus : byte
{
    Submitted = 0,
    UnderReview = 1,
    InvestigationInProgress = 2,
    Closed = 3
}

