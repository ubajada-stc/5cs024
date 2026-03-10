namespace WhistleblowerPlatform.Application.DTOs;

public class SetupKeypairRequest
{
    public byte[] PublicKey { get; set; } = [];
    public byte[] EncryptedPrivateKey { get; set; } = [];
    public byte[] PrivateKeySalt { get; set; } = [];
    public byte[] PrivateKeyIv { get; set; } = [];
}
