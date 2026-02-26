using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Application.Interfaces;

public interface IAttachmentStorageService
{
    Task<string> SaveAsync(Guid reportId, Guid attachmentId, byte[] encryptedContent);
}
