using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Application.Interfaces;

public interface IFileSanitizationService
{
    Task SanitizeAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}
