using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhistleblowerPlatform.Application.Interfaces;

namespace WhistleblowerPlatform.Infrastructure.Services;

public class LocalBlobStorageService : IAttachmentStorageService
{
    private readonly string _basePath;

    public LocalBlobStorageService(string basePath)
    {
        _basePath = basePath;
    }

    public Task<string> SaveAsync(Guid reportId, Guid attachmentId, byte[] encryptedContent)
    {
        return SaveAsync(reportId, attachmentId, encryptedContent, ".enc");
    }

    public async Task<string> SaveAsync(Guid reportId, Guid attachmentId, byte[] encryptedContent, string suffix)
    {
        var directory = Path.Combine(_basePath, "reports", reportId.ToString());
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{attachmentId}{suffix}");
        await File.WriteAllBytesAsync(filePath, encryptedContent);

        return $"reports/{reportId}/{attachmentId}{suffix}";
    }

    public async Task<byte[]> ReadAsync(string storagePath)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task DeleteAsync(string storagePath)
    {
        var fullPath = Path.Combine(_basePath, storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}
