using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using WhistleblowerPlatform.Infrastructure.Sanitization;

namespace WhistleblowerPlatform.Tests;

[TestClass]
public class ImageSanitizerTests
{
    private readonly ImageSanitizer _sanitizer = new();

//Can platform handle the following mime types

    [TestMethod]
    [DataRow("image/jpeg")]
    [DataRow("image/png")]
    [DataRow("image/webp")]
    [DataRow("image/gif")]
    [DataRow("image/bmp")]
    [DataRow("image/tiff")]
    public void CanHandle_SupportedMimeType_ReturnsTrue(string mimeType)
    {
        Assert.IsTrue(_sanitizer.CanHandle(mimeType));
    }

    [TestMethod]
    [DataRow("application/pdf")]
    [DataRow("video/mp4")]
    [DataRow("audio/mpeg")]
    [DataRow("application/octet-stream")]
    public void CanHandle_UnsupportedMimeType_ReturnsFalse(string mimeType)
    {
        Assert.IsFalse(_sanitizer.CanHandle(mimeType));
    }

//Strip metadata from jpg

    [TestMethod]
    public async Task SanitizeAsync_Jpeg_ExifAndGpsMetadataIsStripped()
    {
        // Arrange: create a JPEG with EXIF data including GPS and author fields
        var inputBytes = CreateJpegWithExif();

        // Confirm the test image actually contains EXIF before we sanitize
        using (var before = Image.Load(inputBytes))
        {
            Assert.IsNotNull(before.Metadata.ExifProfile,
                "Test image must contain EXIF data else test is for nothing");
        }

        // Act
        var result = await _sanitizer.SanitizeAsync(inputBytes, "image/jpeg");

        // Assert: output loads as a valid image with no EXIF
        using var after = Image.Load(result.SanitizedContent);
        Assert.IsNull(after.Metadata.ExifProfile,    "ExifProfile (includes GPS, camera, author) must be stripped");
        Assert.IsNull(after.Metadata.IccProfile,     "IccProfile must be stripped");
        Assert.IsNull(after.Metadata.IptcProfile,    "IptcProfile must be stripped");
        Assert.IsNull(after.Metadata.XmpProfile,     "XmpProfile must be stripped");
        Assert.AreEqual("image/jpeg", result.MimeType);
    }

    [TestMethod]
    public async Task SanitizeAsync_Jpeg_PixelDimensionsArePreserved()
    {
        // Arrange
        const int width = 120;
        const int height = 80;
        var inputBytes = CreateJpeg(width, height);

        // Act
        var result = await _sanitizer.SanitizeAsync(inputBytes, "image/jpeg");

        // Assert: image is not corrupted — pixel dimensions unchanged
        using var after = Image.Load(result.SanitizedContent);
        Assert.AreEqual(width,  after.Width,  "Image width must be preserved");
        Assert.AreEqual(height, after.Height, "Image height must be preserved");
        Assert.IsTrue(result.SanitizedContent.Length > 0, "Output must not be empty");
    }

 //Strip meta data from png

    [TestMethod]
    public async Task SanitizeAsync_Png_ExifMetadataIsStripped()
    {
        // Arrange: create a PNG with EXIF data
        var inputBytes = CreatePngWithExif();

        using (var before = Image.Load(inputBytes))
        {
            Assert.IsNotNull(before.Metadata.ExifProfile,
                "Test image must contain EXIF data else test is for nothing");
        }

        // Act
        var result = await _sanitizer.SanitizeAsync(inputBytes, "image/png");

        // Assert
        using var after = Image.Load(result.SanitizedContent);
        Assert.IsNull(after.Metadata.ExifProfile, "ExifProfile must be stripped from PNG");
        Assert.AreEqual("image/png", result.MimeType);
    }

    [TestMethod]
    public async Task SanitizeAsync_Png_PixelDimensionsArePreserved()
    {
        // Arrange
        const int width = 200;
        const int height = 150;
        var inputBytes = CreatePng(width, height);

        // Act
        var result = await _sanitizer.SanitizeAsync(inputBytes, "image/png");

        // Assert
        using var after = Image.Load(result.SanitizedContent);
        Assert.AreEqual(width,  after.Width);
        Assert.AreEqual(height, after.Height);
    }

//helpers to create images with exit

    private static byte[] CreateJpegWithExif()
    {
        using var image = new Image<Rgb24>(100, 100, new Rgb24(128, 64, 32));

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Make,   "TestCamera");
        exif.SetValue(ExifTag.Model,  "TestModel XR100");
        exif.SetValue(ExifTag.Artist, "Benji King");
        // GPS coordinates that should be stripped
        exif.SetValue(ExifTag.GPSLatitude,  new Rational[] { new(51, 1), new(30, 1), new(0, 1) });
        exif.SetValue(ExifTag.GPSLongitude, new Rational[] { new(0, 1),  new(7, 1),  new(39, 1) });

        image.Metadata.ExifProfile = exif;

        using var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder());
        return stream.ToArray();
    }

    private static byte[] CreateJpeg(int width, int height)
    {
        using var image = new Image<Rgb24>(width, height, new Rgb24(100, 149, 237));
        using var stream = new MemoryStream();
        image.Save(stream, new JpegEncoder());
        return stream.ToArray();
    }

    private static byte[] CreatePngWithExif()
    {
        using var image = new Image<Rgba32>(100, 100, new Rgba32(32, 64, 128, 255));

        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Make,   "TestCamera");
        exif.SetValue(ExifTag.Artist, "Belgian Malinois");

        image.Metadata.ExifProfile = exif;

        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, new Rgba32(64, 128, 32, 255));
        using var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        return stream.ToArray();
    }
}
