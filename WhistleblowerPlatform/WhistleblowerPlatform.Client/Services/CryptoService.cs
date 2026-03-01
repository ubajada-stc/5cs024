using Microsoft.JSInterop;

namespace WhistleblowerPlatform.Client.Services;

/// <summary>
/// C# wrapper around the Web Crypto API JavaScript functions.
/// All encryption/decryption happens in the browser — nothing leaves as plaintext.
/// </summary>
public class CryptoService
{
    private readonly IJSRuntime _js;

    public CryptoService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Generate a 32-byte random token (base64)</summary>
    public async Task<string> GenerateTokenAsync()
        => await _js.InvokeAsync<string>("cryptoService.generateToken");

    /// <summary>SHA-256 hash of a token (base64 in, base64 out)</summary>
    public async Task<string> HashTokenAsync(string tokenBase64)
        => await _js.InvokeAsync<string>("cryptoService.hashToken", tokenBase64);

    /// <summary>Generate AES-256-GCM key (base64 of raw bytes)</summary>
    public async Task<string> GenerateSymmetricKeyAsync()
        => await _js.InvokeAsync<string>("cryptoService.generateSymmetricKey");

    /// <summary>Encrypt plaintext with AES-256-GCM</summary>
    public async Task<EncryptedData> EncryptSymmetricAsync(string plaintextBase64, string keyBase64)
        => await _js.InvokeAsync<EncryptedData>("cryptoService.encryptSymmetric", plaintextBase64, keyBase64);

    /// <summary>Generate RSA-OAEP 4096-bit keypair</summary>
    public async Task<KeyPairResult> GenerateKeypairAsync()
        => await _js.InvokeAsync<KeyPairResult>("cryptoService.generateKeypair");

    /// <summary>Encrypt data with RSA-OAEP public key (base64 in, base64 out)</summary>
    public async Task<string> EncryptWithPublicKeyAsync(string dataBase64, string publicKeyBase64)
        => await _js.InvokeAsync<string>("cryptoService.encryptWithPublicKey", dataBase64, publicKeyBase64);

    /// <summary>Encrypt a private key with AES-256-GCM wrapping key</summary>
    public async Task<EncryptedData> EncryptPrivateKeyAsync(string privateKeyBase64, string wrappingKeyBase64)
        => await _js.InvokeAsync<EncryptedData>("cryptoService.encryptPrivateKey", privateKeyBase64, wrappingKeyBase64);

    /// <summary>SHA-256 fingerprint of a public key</summary>
    public async Task<string> FingerprintPublicKeyAsync(string publicKeyBase64)
        => await _js.InvokeAsync<string>("cryptoService.fingerprintPublicKey", publicKeyBase64);

    /// <summary>Convert a plain string to base64</summary>
    public async Task<string> StringToBase64Async(string text)
        => await _js.InvokeAsync<string>("cryptoService.stringToBase64", text);

    /// <summary>Encrypt file content + filename together with AES-256-GCM</summary>
    public async Task<EncryptedData> EncryptFileAsync(string fileContentBase64, string fileNameBase64, string keyBase64)
        => await _js.InvokeAsync<EncryptedData>("cryptoService.encryptFile", fileContentBase64, fileNameBase64, keyBase64);

    public async Task<DualEncryptedFileResult> EncryptFileForSanitizationAsync(
    string fileContentBase64, string fileNameBase64,
    string investigatorPublicKeyBase64, string wbPublicKeyBase64)
    {
        var result = await _js.InvokeAsync<DualEncryptedFileResult>(
            "cryptoService.encryptFileForSanitization",
            fileContentBase64, fileNameBase64,
            investigatorPublicKeyBase64, wbPublicKeyBase64);
        return result;
    }

}

/// <summary>Result of AES-256-GCM encryption</summary>
public class EncryptedData
{
    public string Iv { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public string AuthTag { get; set; } = "";
}

/// <summary>Result of RSA keypair generation</summary>
public class KeyPairResult
{
    public string PublicKey { get; set; } = "";
    public string PrivateKey { get; set; } = "";
}

public class DualEncryptedFileResult
{
    // Sanitization data
    public EncryptedData SanitizationBlob { get; set; } = null!;
    public string SanitizationKey { get; set; } = "";

    // Recipient data
    public EncryptedData RecipientBlob { get; set; } = null!;
    public string KeyEnvelope { get; set; } = "";
    public string WbKeyEnvelope { get; set; } = "";
}