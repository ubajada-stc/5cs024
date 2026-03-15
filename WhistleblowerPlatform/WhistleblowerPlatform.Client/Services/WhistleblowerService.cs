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
}

public class CaseStatusResult
{
    public string CaseNumber { get; set; } = "";
    public byte Status { get; set; }
}

public class WbMailboxResult
{
    public string EncryptedWbPrivateKey { get; set; } = "";
    public byte[] ReportEncryptedContent { get; set; } = [];
    public byte[] ReportWbKeyEnvelope { get; set; } = [];
    public List<WbMessageModel> Messages { get; set; } = new List<WbMessageModel>();
}

public class WbMessageModel
{
    public Guid MessageId { get; set; }
    public byte SenderRole { get; set; }
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] WbKeyEnvelope { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
