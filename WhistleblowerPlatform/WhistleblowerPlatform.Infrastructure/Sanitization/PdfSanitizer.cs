using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhistleblowerPlatform.Application.Interfaces;

namespace WhistleblowerPlatform.Infrastructure.Sanitization;

public class PdfSanitizer : IFileSanitizer
{
    public bool CanHandle(string mimeType)
    {
        return string.Equals(mimeType, "application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public Task<SanitizationResult> SanitizeAsync(
        byte[] fileContent,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement PDF metadata stripping
        // For now, pass through unchanged.
        // The background service will mark this as "Completed" but a future
        // implementation should actually strip the /Info dictionary and XMP.

        return Task.FromResult(new SanitizationResult
        {
            SanitizedContent = fileContent,
            MimeType = mimeType
        });
    }
}
