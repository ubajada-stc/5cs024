namespace WhistleblowerPlatform.Client.Services;

public class InvestigatorKeyService
{
    public string? PrivateKeyBase64 { get; private set; }
    public bool HasPrivateKey => PrivateKeyBase64 != null;

    public void SetPrivateKey(string privateKeyBase64) => PrivateKeyBase64 = privateKeyBase64;

    public void Clear() => PrivateKeyBase64 = null;
}
