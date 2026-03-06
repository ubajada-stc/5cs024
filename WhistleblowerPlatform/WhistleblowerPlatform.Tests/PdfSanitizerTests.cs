using iText.Kernel.Pdf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhistleblowerPlatform.Infrastructure.Sanitization;

namespace WhistleblowerPlatform.Tests;

[TestClass]
public class PdfSanitizerTests
{
    private readonly PdfSanitizer _sanitizer = new();

    // -------------------------------------------------------------------------
    // CanHandle
    // -------------------------------------------------------------------------

    [TestMethod]
    public void CanHandle_ApplicationPdf_ReturnsTrue()
    {
        Assert.IsTrue(_sanitizer.CanHandle("application/pdf"));
    }

    [TestMethod]
    [DataRow("image/jpeg")]
    [DataRow("video/mp4")]
    [DataRow("application/octet-stream")]
    public void CanHandle_UnsupportedMimeType_ReturnsFalse(string mimeType)
    {
        Assert.IsFalse(_sanitizer.CanHandle(mimeType));
    }

    // -------------------------------------------------------------------------
    // Metadata stripped
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task SanitizeAsync_Pdf_InfoDictionaryIsCleared()
    {
        // Arrange: create a PDF with author-identifying metadata
        var inputBytes = CreatePdfWithMetadata(
            title:    "Confidential Evidence",
            author:   "John Whistleblower",
            subject:  "Misconduct Report",
            keywords: "corruption, fraud",
            creator:  "Microsoft Word");

        // Confirm test PDF actually contains metadata before sanitizing
        using (var reader = new PdfReader(new MemoryStream(inputBytes)))
        using (var pdfDoc = new PdfDocument(reader))
        {
            var info = pdfDoc.GetDocumentInfo();
            Assert.AreEqual("John Whistleblower", info.GetAuthor(),
                "Test PDF must have author metadata for this test to be meaningful");
        }

        // Act
        var result = await _sanitizer.SanitizeAsync(inputBytes, "application/pdf");

        // Assert: all identifying fields are cleared
        using var outReader = new PdfReader(new MemoryStream(result.SanitizedContent));
        using var outDoc = new PdfDocument(outReader);
        var outInfo = outDoc.GetDocumentInfo();

        Assert.IsTrue(string.IsNullOrEmpty(outInfo.GetTitle()),    "Title must be cleared");
        Assert.IsTrue(string.IsNullOrEmpty(outInfo.GetAuthor()),   "Author must be cleared");
        Assert.IsTrue(string.IsNullOrEmpty(outInfo.GetSubject()),  "Subject must be cleared");
        Assert.IsTrue(string.IsNullOrEmpty(outInfo.GetKeywords()), "Keywords must be cleared");
        Assert.IsTrue(string.IsNullOrEmpty(outInfo.GetCreator()),  "Creator must be cleared");
        Assert.AreEqual("application/pdf", result.MimeType);
    }

    [TestMethod]
    public async Task SanitizeAsync_Pdf_XmpMetadataStreamIsRemoved()
    {
        // Arrange
        var inputBytes = CreatePdfWithMetadata(author: "Jane Doe");

        // Act
        var result = await _sanitizer.SanitizeAsync(inputBytes, "application/pdf");

        // Assert: catalog has no /Metadata entry
        using var reader = new PdfReader(new MemoryStream(result.SanitizedContent));
        using var pdfDoc = new PdfDocument(reader);
        var metadataEntry = pdfDoc.GetCatalog().GetPdfObject().Get(PdfName.Metadata);
        Assert.IsNull(metadataEntry, "XMP metadata stream must be removed from catalog");
    }

    [TestMethod]
    public async Task SanitizeAsync_Pdf_OutputIsValidPdfWithCorrectPageCount()
    {
        // Arrange: 3-page PDF
        var inputBytes = CreatePdfWithMetadata(pageCount: 3);

        // Act
        var result = await _sanitizer.SanitizeAsync(inputBytes, "application/pdf");

        // Assert: output opens as a valid PDF and page count is preserved
        Assert.IsTrue(result.SanitizedContent.Length > 0, "Output must not be empty");

        using var reader = new PdfReader(new MemoryStream(result.SanitizedContent));
        using var pdfDoc = new PdfDocument(reader);
        Assert.AreEqual(3, pdfDoc.GetNumberOfPages(), "Page count must be preserved");
    }

    // -------------------------------------------------------------------------
    // Helper — programmatic test PDF (no external files needed)
    // -------------------------------------------------------------------------

    private static byte[] CreatePdfWithMetadata(
        string title    = "",
        string author   = "",
        string subject  = "",
        string keywords = "",
        string creator  = "",
        int    pageCount = 1)
    {
        using var outputStream = new MemoryStream();
        using var writer = new PdfWriter(outputStream);
        using var pdfDoc = new PdfDocument(writer);

        var info = pdfDoc.GetDocumentInfo();
        if (!string.IsNullOrEmpty(title))    info.SetTitle(title);
        if (!string.IsNullOrEmpty(author))   info.SetAuthor(author);
        if (!string.IsNullOrEmpty(subject))  info.SetSubject(subject);
        if (!string.IsNullOrEmpty(keywords)) info.SetKeywords(keywords);
        if (!string.IsNullOrEmpty(creator))  info.SetCreator(creator);

        for (var i = 0; i < pageCount; i++)
            pdfDoc.AddNewPage();

        pdfDoc.Close();
        return outputStream.ToArray();
    }
}
