namespace WhistleblowerPlatform.Client.Services;

public class InvestigatorKeyService
{
    public string? PrivateKeyBase64 { get; private set; }
    public string? PublicKeyBase64 { get; private set; }
    public bool HasPrivateKey => PrivateKeyBase64 != null;

    public void SetPrivateKey(string privateKeyBase64) => PrivateKeyBase64 = privateKeyBase64;

    public void SetPublicKey(string publicKeyBase64) => PublicKeyBase64 = publicKeyBase64;

    public void Clear()
    {
        PrivateKeyBase64 = null;
        PublicKeyBase64 = null;
    }
}
