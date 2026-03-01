using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Application.Interfaces;

public interface IFileSanitizer
{
    bool CanHandle(string mimeType);

    Task<SanitizationResult> SanitizeAsync(byte[] fileContent, string mimeType, CancellationToken cancellationToken = default);


}

public class SanitizationResult
{
    public required byte[] SanitizedContent { get; init; }

    public required string MimeType { get; init; }

    public string? NewFileExtension { get; init; }
}
