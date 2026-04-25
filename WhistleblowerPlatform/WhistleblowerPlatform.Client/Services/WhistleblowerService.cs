using System.Net.Http.Json;

namespace WhistleblowerPlatform.Client.Services;

public class WhistleblowerService
{
    private readonly HttpClient _http;

    public WhistleblowerService(HttpClient http)
    {
        _http = http;
    }

    public async Task<CaseStatusResult?> GetCaseAsync(string tokenHashBase64)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/whistleblower/case");
        req.Headers.Add("X-WB-Token", tokenHashBase64);
        using var resp = await _http.SendAsync(req);
        if (resp.StatusCode == System.Net.HttpStatusCode.Gone)
            throw new Exception("This case has been permanently deleted.");
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadFromJsonAsync<CaseStatusResult>();
    }
    public async Task<WbMailboxResult?> GetMessagesAsync(string tokenHashBase64)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/whistleblower/messages");
        req.Headers.Add("X-WB-Token", tokenHashBase64);
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<WbMailboxResult>();
    }

    public async Task<string?> GetInvestigatorPublicKeyAsync()
    {
        var resp = await _http.GetFromJsonAsync<PublicKeyResult>("/api/config/public-key");
        return resp?.PublicKey;
    }

    public async Task<WbAttachmentBlobModel?> GetAttachmentAsync(string tokenHashBase64, Guid attachmentId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/whistleblower/attachments/{attachmentId}");
        req.Headers.Add("X-WB-Token", tokenHashBase64);
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<WbAttachmentBlobModel>();
    }

    public async Task<SendReplyResult?> SendReplyAsync(string tokenHashBase64, SendWbMessageRequest request)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/whistleblower/messages");
        req.Headers.Add("X-WB-Token", tokenHashBase64);
        req.Content = JsonContent.Create(request);
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<SendReplyResult>();
    }
}

public class CaseStatusResult
{
    public string CaseNumber { get; set; } = "";
    public byte Status { get; set; }
}

public class WbMailboxResult
{
    public string EncryptedWbPrivateKey { get; set; } = "";
    public string WbPublicKey { get; set; } = "";
    public byte[] ReportEncryptedContent { get; set; } = [];
    public byte[] ReportWbKeyEnvelope { get; set; } = [];
    public List<WbMessageModel> Messages { get; set; } = new List<WbMessageModel>();
    public List<WbAttachmentModel> Attachments { get; set; } = new List<WbAttachmentModel>();
}

public class WbAttachmentModel
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public byte SanitizationStatus { get; set; }
    public byte[] WbKeyEnvelope { get; set; } = [];
}

public class WbAttachmentBlobModel
{
    public string Iv { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public string AuthTag { get; set; } = "";
    public string WbKeyEnvelope { get; set; } = "";
    public string MimeType { get; set; } = "";
    public string FileName { get; set; } = "";
    public byte SanitizationStatus { get; set; }
}

public class WbMessageModel
{
    public Guid MessageId { get; set; }
    public byte SenderRole { get; set; }
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] WbKeyEnvelope { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class SendWbMessageRequest
{
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
    public byte[]? WbKeyEnvelope { get; set; }
}

public class SendReplyResult
{
    public Guid MessageId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PublicKeyResult
{
    public string PublicKey { get; set; } = "";
}
