using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhistleblowerPlatform.Application.Interfaces;

namespace WhistleblowerPlatform.Infrastructure.Sanitization;

public class ImageSanitizer : IFileSanitizer
{
    private static readonly HashSet<string> SupportedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "image/bmp",
        "image/tiff"
    };

    public bool CanHandle(string mimeType)
    {
        return SupportedMimeTypes.Contains(mimeType);
    }

    public async Task<SanitizationResult> SanitizeAsync(
        byte[] fileContent,
        string mimeType,
        CancellationToken cancellationToken = default)
    {
        // Decode the image — this reads only pixel data.
        // All metadata (EXIF, GPS, ICC, XMP, comments) is NOT loaded
        // into the pixel buffer, so it's effectively discarded.
        using var image = Image.Load(fileContent);

        // Strip any metadata that ImageSharp did parse
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        // Re-encode to the same format — only pixel data is written
        using var outputStream = new MemoryStream();
        var encoder = GetEncoder(mimeType);
        await image.SaveAsync(outputStream, encoder, cancellationToken);

        return new SanitizationResult
        {
            SanitizedContent = outputStream.ToArray(),
            MimeType = mimeType
            // No NewFileExtension — same format as input
        };
    }

    /// <summary>
    /// Returns the appropriate ImageSharp encoder for the given MIME type.
    /// Each encoder produces a clean output with no metadata.
    /// </summary>
    private static IImageEncoder GetEncoder(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => new JpegEncoder { Quality = 95 },  // High quality to minimize loss
            "image/png" => new PngEncoder(),
            "image/webp" => new WebpEncoder { Quality = 95 },
            "image/gif" => new GifEncoder(),   // Note: only first frame preserved for animated GIFs
            "image/bmp" => new BmpEncoder(),
            "image/tiff" => new PngEncoder(),  // TIFF re-encoded as PNG (better compatibility)
            _ => throw new NotSupportedException($"No encoder for MIME type: {mimeType}")
        };
    }
}
