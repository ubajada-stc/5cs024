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
}

public class CaseStatusResult
{
    public string CaseNumber { get; set; } = "";
    public byte Status { get; set; }
}
