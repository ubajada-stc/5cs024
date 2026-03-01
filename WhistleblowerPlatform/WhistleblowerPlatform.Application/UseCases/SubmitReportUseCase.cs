using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Domain.Enums;
using UUIDNext;

namespace WhistleblowerPlatform.Application.UseCases;

public class SubmitReportUseCase
{
    private readonly IReportRepository _reportRepository;
    private readonly IAttachmentStorageService _attachmentStorage;

    public SubmitReportUseCase(IReportRepository reportRepository, IAttachmentStorageService attachmentStorage)
    {
        _reportRepository = reportRepository;
        _attachmentStorage = attachmentStorage;
    }

    public async Task<SubmitReportResult> ExecuteAsync(SubmitReportRequest request)
    {
        var validationErrors = Validators.SubmitReportValidator.Validate(request);
        if (validationErrors.Count > 0)
            throw new ArgumentException(string.Join(" ", validationErrors));

        if (request.CategoryId.HasValue)
        {
            var categoryExists = await _reportRepository.CategoryExistsAsync(request.CategoryId.Value);
            if (!categoryExists)
            {
                throw new ArgumentException("Invalid category ID");

            }
        }

        var caseNumber = await _reportRepository.GenerateCaseNumberAsync();

        var now = DateTime.UtcNow;
        var acknowledgementDueAt = now.AddDays(3);
        var feedbackDueAt = now.AddMonths(3);

        var reportId = Uuid.NewSequential();
        var report = new Report
        {
            ReportId = reportId,
            CaseNumber = caseNumber,
            TokenHash = request.TokenHash,
            CategoryId = request.CategoryId,
            Status = ReportStatus.Submitted,
            EncryptedContent = request.EncryptedContent,
            EncryptedKeyEnvelope = request.EncryptedKeyEnvelope,
            WbkeyEnvelope = request.WbKeyEnvelope,
            WbpublicKey = request.WbPublicKey,
            EncryptedWbprivateKey = request.EncryptedWbPrivateKey,
            WbkeySalt = request.WbKeySalt,
            SelfIdentified = request.SelfIdentified,
            EncryptedIdentity = request.EncryptedIdentity,
            EncryptedIdentityKeyEnvelope = request.EncryptedIdentityKeyEnvelope,
            AcknowledgementDueAt = acknowledgementDueAt,
            FeedbackDueAt = feedbackDueAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (var file in request.Attachments)
        {
            var attachmentId = Uuid.NewSequential();

            var storagePath = await _attachmentStorage.SaveAsync(reportId, attachmentId, file.EncryptedContent);

            report.ReportAttachments.Add(new ReportAttachment
            {
                AttachmentId = attachmentId,
                ReportId = reportId,
                StoragePath = storagePath,
                EncryptedKeyEnvelope = file.EncryptedKeyEnvelope,
                WbkeyEnvelope = file.WbKeyEnvelope,
                EncryptedFileName = file.EncryptedFileName,
                MimeType = file.MimeType,   
                FileSize = file.FileSize,
                CreatedAt = now
            }
            );
        }


        await _reportRepository.AddAsync(report);

        return new SubmitReportResult
        {
            CaseNumber = caseNumber
        };
    }
}
