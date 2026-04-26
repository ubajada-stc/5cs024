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
        using var inputStream = new MemoryStream(fileContent);
        using var outputStream = new MemoryStream();

        var readerProps = new ReaderProperties().SetUnethicalReading(true);
        using var reader = new PdfReader(inputStream, readerProps);
        using var writer = new PdfWriter(outputStream);
        using var pdfDoc = new PdfDocument(reader, writer);

        // Strip /Info dictionary — contains author, title, subject, keywords, creator
        var info = pdfDoc.GetDocumentInfo();
        info.SetTitle(string.Empty);
        info.SetAuthor(string.Empty);
        info.SetSubject(string.Empty);
        info.SetKeywords(string.Empty);
        info.SetCreator(string.Empty);

        // Remove the XMP metadata stream from the document catalog
        pdfDoc.GetCatalog().GetPdfObject().Remove(PdfName.Metadata);

        pdfDoc.Close();

        return Task.FromResult(new SanitizationResult
        {
            SanitizedContent = outputStream.ToArray(),
            MimeType = mimeType
        });
    }
}
