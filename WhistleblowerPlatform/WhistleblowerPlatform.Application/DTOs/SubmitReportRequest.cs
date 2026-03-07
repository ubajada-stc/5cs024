using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Application.DTOs;

public class SubmitReportRequest
{
    public required byte[] TokenHash { get; set; }
    public int? CategoryId { get; set; }
    public required byte[] EncryptedContent { get; set; }
    public required byte[] EncryptedKeyEnvelope { get; set; }
    public required byte[] WbKeyEnvelope { get; set; }
    public required byte[] WbPublicKey { get; set; }
    public required byte[] EncryptedWbPrivateKey { get; set; }
    public required byte[] WbKeySalt { get; set; }
    public bool SelfIdentified { get; set; }
    public byte[]? EncryptedIdentity { get; set; }
    public byte[]? EncryptedIdentityKeyEnvelope { get; set; }
    public string HCaptchaToken { get; set; } = string.Empty;


    public List<SubmitAttachmentRequest> Attachments { get; set; } = new();


}

