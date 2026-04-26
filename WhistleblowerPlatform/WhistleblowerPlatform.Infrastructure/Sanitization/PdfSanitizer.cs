using iText.Kernel.Pdf;
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
        if (fileContent.Length < 4 ||
            System.Text.Encoding.ASCII.GetString(fileContent, 0, 4) != "%PDF")
        {
            throw new InvalidOperationException(
                $"Invalid PDF header. First bytes: {BitConverter.ToString(fileContent, 0, Math.Min(8, fileContent.Length))}");
        }

        using var inputStream = new MemoryStream(fileContent);
        using var outputStream = new MemoryStream();

        using var reader = new PdfReader(inputStream);
        using var srcDoc = new PdfDocument(reader);

        using var writer = new PdfWriter(outputStream);
        using var destDoc = new PdfDocument(writer);

        // Copy all pages into a fresh document (avoids stamping-mode issues)
        srcDoc.CopyPagesTo(1, srcDoc.GetNumberOfPages(), destDoc);

        // Strip /Info dictionary — contains author, title, subject, keywords, creator
        var info = destDoc.GetDocumentInfo();
        info.SetTitle(string.Empty);
        info.SetAuthor(string.Empty);
        info.SetSubject(string.Empty);
        info.SetKeywords(string.Empty);
        info.SetCreator(string.Empty);

        // Remove the XMP metadata stream from the document catalog
        destDoc.GetCatalog().GetPdfObject().Remove(PdfName.Metadata);

        destDoc.Close();

        return Task.FromResult(new SanitizationResult
        {
            SanitizedContent = outputStream.ToArray(),
            MimeType = mimeType
        });
    }
}
