using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Enums;
using WhistleblowerPlatform.Infrastructure.Persistence;

namespace WhistleblowerPlatform.Infrastructure.Sanitization;

public class FileSanitizationService : IFileSanitizationService
{
    private readonly WhistleblowerDbContext _dbContext;
    private readonly IAttachmentStorageService _storageService;
    private readonly IEnumerable<IFileSanitizer> _sanitizers;
    private readonly ILogger<FileSanitizationService> _logger;

    public FileSanitizationService(
        WhistleblowerDbContext dbContext,
        IAttachmentStorageService storageService,
        IEnumerable<IFileSanitizer> sanitizers,
        ILogger<FileSanitizationService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _sanitizers = sanitizers;
        _logger = logger;
    }

    public async Task SanitizeAttachmentAsync(
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        // =========================================================
        // STEP 1: LOAD ATTACHMENT RECORD
        // =========================================================

        var attachment = await _dbContext.ReportAttachments
            .Include(a => a.Report)
            .FirstOrDefaultAsync(a => a.AttachmentId == attachmentId, cancellationToken);

        if (attachment == null)
        {
            _logger.LogWarning("Attachment {AttachmentId} not found — skipping sanitization", attachmentId);
            return;
        }

        if (attachment.SanitizationStatus != SanitizationStatus.Queued)
        {
            _logger.LogWarning("Attachment {AttachmentId} is not in Queued state — skipping", attachmentId);
            return;
        }

        if (attachment.SanitizationKey == null || attachment.SanitizationStoragePath == null)
        {
            _logger.LogWarning("Attachment {AttachmentId} missing sanitization key or path — marking as Failed", attachmentId);
            await MarkFailedAsync(attachment, "Missing sanitization key or storage path", cancellationToken);
            return;
        }

        // Mark as processing
        attachment.SanitizationStatus = SanitizationStatus.Processing;
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Track plaintext byte arrays for explicit clearing
        byte[]? plaintextPayload = null;
        byte[]? sanitizedContent = null;
        byte[]? sanitizedPayload = null;
        byte[]? newSymmetricKey = null;

        try
        {
            // =========================================================
            // STEP 2: READ AND DECRYPT THE SANITIZATION BLOB
            // =========================================================

            var encryptedBlob = await _storageService.ReadAsync(attachment.SanitizationStoragePath);

            // The sanitization blob is stored as: IV (12 bytes) + Ciphertext + AuthTag (16 bytes)
            // This matches the AES-256-GCM format from the client
            //plaintextPayload = DecryptAesGcm(encryptedBlob, attachment.SanitizationKey);
            plaintextPayload = DecryptSanitizationBlob(encryptedBlob, attachment.SanitizationKey);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var fileData = JsonSerializer.Deserialize<FilePayload>(plaintextPayload, options);
            if (fileData == null || fileData.Content == null)
            {
                await MarkFailedAsync(attachment, "Failed to deserialize file payload", cancellationToken);
                return;
            }

            // Content is a base64 string — decode to raw bytes for the sanitizer
            var fileContent = Convert.FromBase64String(fileData.Content);
            var fileName = fileData.FileName ?? "unknown";

            // =========================================================
            // STEP 3: SELECT SANITIZER AND STRIP METADATA
            // =========================================================

            var sanitizer = _sanitizers.FirstOrDefault(s => s.CanHandle(attachment.MimeType));

            if (sanitizer == null)
            {
                // No sanitizer available for this file type
                _logger.LogInformation(
                    "No sanitizer for MIME type {MimeType} — skipping attachment {AttachmentId}",
                    attachment.MimeType, attachmentId);

                await MarkSkippedAsync(attachment, cancellationToken);
                return;
            }

            var result = await sanitizer.SanitizeAsync(fileContent, attachment.MimeType, cancellationToken);
            sanitizedContent = result.SanitizedContent;

            // Update filename if the format changed (e.g., docx → pdf)
            if (result.NewFileExtension != null)
            {
                fileName = Path.ChangeExtension(fileName, result.NewFileExtension);
            }

            // =========================================================
            // STEP 4: RE-ENCRYPT FOR RECIPIENTS
            // =========================================================

            // Build the sanitized payload in the same format the client uses
            sanitizedPayload = JsonSerializer.SerializeToUtf8Bytes(new FilePayload
            {
                FileName = fileName,
                Content = Convert.ToBase64String(sanitizedContent)
            });

            // Generate a new AES-256-GCM symmetric key
            newSymmetricKey = RandomNumberGenerator.GetBytes(32);

            // Encrypt the sanitized payload
            var encryptedSanitized = EncryptAesGcm(sanitizedPayload, newSymmetricKey);

            // Encrypt the new symmetric key for the investigator (RSA-OAEP)
            var investigator = await _dbContext.Investigators
                .Where(i => i.IsActive)
                .Select(i => new { i.PublicKey })
                .FirstOrDefaultAsync(cancellationToken);

            if (investigator == null)
            {
                await MarkFailedAsync(attachment, "No active investigator found for key envelope", cancellationToken);
                return;
            }

            var newKeyEnvelope = RsaOaepEncrypt(newSymmetricKey, investigator.PublicKey);

            // Encrypt the new symmetric key for the whistleblower
            var wbPublicKey = attachment.Report.WbpublicKey;
            var newWbKeyEnvelope = RsaOaepEncrypt(newSymmetricKey, wbPublicKey);

            // =========================================================
            // STEP 5: REPLACE STORED BLOB AND UPDATE RECORD
            // =========================================================

            // Overwrite the recipient blob with the sanitized version
            // We reuse the same StoragePath — the old blob is replaced
            await _storageService.SaveAsync(
                attachment.ReportId,
                attachment.AttachmentId,
                encryptedSanitized);

            // Update the attachment record
            attachment.EncryptedKeyEnvelope = newKeyEnvelope;
            attachment.WbkeyEnvelope = newWbKeyEnvelope;
            attachment.MimeType = result.MimeType;
            attachment.FileSize = encryptedSanitized.Length;
            attachment.SanitizationStatus = SanitizationStatus.Completed;
            attachment.SanitizedAt = DateTime.UtcNow;

            // =========================================================
            // STEP 6: CLEAN UP — DELETE SANITIZATION DATA
            // =========================================================

            // Delete the sanitization blob from blob storage
            await _storageService.DeleteAsync(attachment.SanitizationStoragePath);

            // Clear sanitization key and path from the database record
            attachment.SanitizationKey = null;
            attachment.SanitizationStoragePath = null;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sanitization completed for attachment {AttachmentId} (type: {MimeType})",
                attachmentId, attachment.MimeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sanitization failed for attachment {AttachmentId}", attachmentId);
            await MarkFailedAsync(attachment, ex.Message, cancellationToken);
        }
        finally
        {
            // =========================================================
            // SECURITY: Explicitly clear all plaintext from memory
            // =========================================================
            // Array.Clear overwrites bytes with zeros.
            // This minimises the window where plaintext exists in memory.
            // Not a perfect guarantee (GC may have copied arrays) but
            // it's the best we can do in managed code.

            if (plaintextPayload != null) Array.Clear(plaintextPayload);
            if (sanitizedContent != null) Array.Clear(sanitizedContent);
            if (sanitizedPayload != null) Array.Clear(sanitizedPayload);
            if (newSymmetricKey != null) Array.Clear(newSymmetricKey);
        }
    }

    // =================================================================
    // HELPER: AES-256-GCM Decryption
    // =================================================================

    /// <summary>
    /// Decrypts an AES-256-GCM encrypted blob.
    /// Expected format: IV (12 bytes) || Ciphertext || AuthTag (16 bytes)
    /// </summary>
    private static byte[] DecryptAesGcm(byte[] encryptedBlob, byte[] key)
    {
        const int ivLength = 12;
        const int tagLength = 16;

        if (encryptedBlob.Length < ivLength + tagLength)
            throw new CryptographicException("Encrypted blob too short");

        var iv = encryptedBlob[..ivLength];
        var tag = encryptedBlob[^tagLength..];
        var ciphertext = encryptedBlob[ivLength..^tagLength];

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, tagLength);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return plaintext;
    }

    // =================================================================
    // HELPER: AES-256-GCM Encryption
    // =================================================================

    /// <summary>
    /// Encrypts plaintext with AES-256-GCM.
    /// Returns: IV (12 bytes) || Ciphertext || AuthTag (16 bytes)
    /// </summary>
    private static byte[] EncryptAesGcm(byte[] plaintext, byte[] key)
    {
        const int ivLength = 12;
        const int tagLength = 16;

        var iv = RandomNumberGenerator.GetBytes(ivLength);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[tagLength];

        using var aes = new AesGcm(key, tagLength);
        aes.Encrypt(iv, plaintext, ciphertext, tag);

        // Concatenate: IV || Ciphertext || Tag
        var result = new byte[ivLength + ciphertext.Length + tagLength];
        iv.CopyTo(result, 0);
        ciphertext.CopyTo(result, ivLength);
        tag.CopyTo(result, ivLength + ciphertext.Length);

        return result;
    }

    // =================================================================
    // HELPER: RSA-OAEP Encryption (for key envelopes)
    // =================================================================

    /// <summary>
    /// Encrypts a symmetric key with an RSA public key using OAEP-SHA256.
    /// </summary>
    private static byte[] RsaOaepEncrypt(byte[] data, byte[] publicKeyBytes)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
        return rsa.Encrypt(data, RSAEncryptionPadding.OaepSHA256);
    }

    // =================================================================
    // HELPER: Mark attachment as failed/skipped
    // =================================================================

    private async Task MarkFailedAsync(
        Domain.Entities.ReportAttachment attachment,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        attachment.SanitizationStatus = SanitizationStatus.Failed;
        attachment.SanitizationError = errorMessage.Length > 500
            ? errorMessage[..500]
            : errorMessage;
        attachment.SanitizedAt = DateTime.UtcNow;

        // Keep the original recipient-encrypted blob — investigator can still access it
        // but should see a warning that metadata may be present.
        // Still delete the sanitization key and blob for security.
        await CleanupSanitizationDataAsync(attachment, cancellationToken);
    }

    private async Task MarkSkippedAsync(
        Domain.Entities.ReportAttachment attachment,
        CancellationToken cancellationToken)
    {
        attachment.SanitizationStatus = SanitizationStatus.Skipped;
        attachment.SanitizedAt = DateTime.UtcNow;

        await CleanupSanitizationDataAsync(attachment, cancellationToken);
    }

    private async Task CleanupSanitizationDataAsync(
        Domain.Entities.ReportAttachment attachment,
        CancellationToken cancellationToken)
    {
        // Always delete sanitization data regardless of outcome
        if (attachment.SanitizationStoragePath != null)
        {
            try
            {
                await _storageService.DeleteAsync(attachment.SanitizationStoragePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete sanitization blob for {AttachmentId}", attachment.AttachmentId);
            }
        }

        attachment.SanitizationKey = null;
        attachment.SanitizationStoragePath = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static byte[] DecryptSanitizationBlob(byte[] storedBlob, byte[] key)
    {
        var json = System.Text.Encoding.UTF8.GetString(storedBlob);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var parts = System.Text.Json.JsonSerializer.Deserialize<EncryptedBlobParts>(json, options);

        if (parts == null || parts.Iv == null || parts.Ciphertext == null || parts.AuthTag == null)
            throw new CryptographicException("Invalid sanitization blob format");

        var iv = Convert.FromBase64String(parts.Iv);
        var ciphertext = Convert.FromBase64String(parts.Ciphertext);
        var tag = Convert.FromBase64String(parts.AuthTag);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return plaintext;
    }
}

/// <summary>
/// Internal DTO matching the JSON structure the client produces for file payloads.
/// { "fileName": "photo.jpg", "content": [byte array] }
/// </summary>
internal class FilePayload
{
    public string? FileName { get; set; }
    public string? Content { get; set; }
}



internal class EncryptedBlobParts
{
    public string? Iv { get; set; }

 
    public string? Ciphertext { get; set; }

 
    public string? AuthTag { get; set; }
}
