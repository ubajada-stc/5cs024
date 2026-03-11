using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WhistleblowerPlatform.Client.Services;

public class InvestigatorReportService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public InvestigatorReportService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    public async Task<List<CaseListItem>> GetCasesAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/investigator/cases");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return [];
        return await resp.Content.ReadFromJsonAsync<List<CaseListItem>>() ?? [];
    }

    public async Task<CaseDetailModel?> GetCaseDetailAsync(string caseNumber)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/investigator/cases/{Uri.EscapeDataString(caseNumber)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<CaseDetailModel>();
    }

    public async Task<AttachmentBlobModel?> GetAttachmentBlobAsync(string caseNumber, Guid attachmentId)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"/api/investigator/cases/{Uri.EscapeDataString(caseNumber)}/attachments/{attachmentId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<AttachmentBlobModel>();
    }
}

public class CaseListItem
{
    public string CaseNumber { get; set; } = "";
    public byte Status { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime AcknowledgementDueAt { get; set; }
    public DateTime FeedbackDueAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public int AttachmentCount { get; set; }
}

public class CaseDetailModel
{
    public string CaseNumber { get; set; } = "";
    public byte Status { get; set; }
    public string? CategoryName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime AcknowledgementDueAt { get; set; }
    public DateTime FeedbackDueAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public bool SelfIdentified { get; set; }
    public byte[] EncryptedContent { get; set; } = [];
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
    public byte[]? EncryptedIdentity { get; set; }
    public byte[]? EncryptedIdentityKeyEnvelope { get; set; }
    public List<AttachmentModel> Attachments { get; set; } = [];
}

public class AttachmentModel
{
    public Guid AttachmentId { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public long FileSize { get; set; }
    public byte SanitizationStatus { get; set; }
    public string? SanitizationError { get; set; }
    public byte[] EncryptedKeyEnvelope { get; set; } = [];
}

public class AttachmentBlobModel
{
    public string Iv { get; set; } = "";
    public string Ciphertext { get; set; } = "";
    public string AuthTag { get; set; } = "";
    public string EncryptedKeyEnvelope { get; set; } = "";
    public string MimeType { get; set; } = "";
    public string FileName { get; set; } = "";
    public byte SanitizationStatus { get; set; }
}
