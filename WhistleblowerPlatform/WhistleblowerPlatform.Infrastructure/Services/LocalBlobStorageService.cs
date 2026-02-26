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

    public async Task<string> SaveAsync(Guid reportId, Guid attachmentId, byte[] encryptedContent)
    {
        var directory = Path.Combine(_basePath, "report", reportId.ToString());
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{attachmentId}.enc");
        await File.WriteAllBytesAsync(filePath, encryptedContent);

        return $"/reports/{reportId}/{attachmentId}.enc";
    }
}
