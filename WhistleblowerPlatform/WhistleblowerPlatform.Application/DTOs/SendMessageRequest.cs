namespace WhistleblowerPlatform.Application.DTOs;

public class SendMessageRequest
{
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
    public byte[] WbKeyEnvelope { get; set; } = [];
}
