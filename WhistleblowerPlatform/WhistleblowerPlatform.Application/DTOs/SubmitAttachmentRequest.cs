using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Application.DTOs;

public class SubmitAttachmentRequest
{
    public required byte[] EncryptedContent { get; set; }
    public required byte[] EncryptedKeyEnvelope { get; set; }
    public required byte[] WbKeyEnvelope { get; set; }
    public required byte[] EncryptedFileName { get; set; }

    public required string MimeType { get; set; }

    public required long FileSize { get; set; }
}
