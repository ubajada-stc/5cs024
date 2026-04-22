using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WhistleblowerPlatform.Client.Services;

public class AdminService
{
    private readonly HttpClient _http;
    private readonly AuthService _auth;

    public AdminService(HttpClient http, AuthService auth)
    {
        _http = http;
        _auth = auth;
    }

    // ── US-4.1: Investigators ─────────────────────────────────────────────────

    public async Task<List<InvestigatorListItem>> GetInvestigatorsAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/investigators");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode
            ? await resp.Content.ReadFromJsonAsync<List<InvestigatorListItem>>() ?? new()
            : new();
    }

    public async Task<(bool Success, string? Error)> CreateInvestigatorAsync(string email, string password)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/admin/investigators");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        req.Content = JsonContent.Create(new { Email = email, Password = password });
        using var resp = await _http.SendAsync(req);
        if (resp.IsSuccessStatusCode) return (true, null);
        var body = await resp.Content.ReadAsStringAsync();
        return (false, body);
    }

    public async Task<bool> UpdateInvestigatorEmailAsync(Guid id, string email)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/investigators/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        req.Content = JsonContent.Create(new { Email = email });
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivateInvestigatorAsync(Guid id)
    {
        using var req = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/investigators/{id}/deactivate");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    // ── US-4.2: Categories ────────────────────────────────────────────────────

    public async Task<List<CategoryListItem>> GetCategoriesAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/categories");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode
            ? await resp.Content.ReadFromJsonAsync<List<CategoryListItem>>() ?? new()
            : new();
    }

    public async Task<bool> CreateCategoryAsync(string name, string? description, int displayOrder)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/admin/categories");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        req.Content = JsonContent.Create(new { Name = name, Description = description, DisplayOrder = displayOrder });
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateCategoryAsync(int id, string name, string? description, int displayOrder)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/categories/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        req.Content = JsonContent.Create(new { Name = name, Description = description, DisplayOrder = displayOrder });
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> DeactivateCategoryAsync(int id)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/categories/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    // ── US-4.3: Audit Logs ────────────────────────────────────────────────────

    // ── US-4.4 + US-4.6: Settings ─────────────────────────────────────────────

    public async Task<List<SettingItem>> GetSettingsAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/settings");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode
            ? await resp.Content.ReadFromJsonAsync<List<SettingItem>>() ?? new()
            : new();
    }

    public async Task<bool> UpdateSettingAsync(string key, string value)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/settings/{Uri.EscapeDataString(key)}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        req.Content = JsonContent.Create(new { Value = value });
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    // ── US-4.3: Audit Logs ────────────────────────────────────────────────────

    public async Task<AuditLogPage> GetAuditLogsAsync(byte? actorType, string? action, DateTime? from, DateTime? to, int page, int pageSize)
    {
        var url = $"/api/admin/audit-logs?page={page}&pageSize={pageSize}";
        if (actorType.HasValue) url += $"&actorType={actorType.Value}";
        if (!string.IsNullOrEmpty(action)) url += $"&action={Uri.EscapeDataString(action)}";
        if (from.HasValue) url += $"&from={from.Value:O}";
        if (to.HasValue) url += $"&to={to.Value:O}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.AccessToken);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode
            ? await resp.Content.ReadFromJsonAsync<AuditLogPage>() ?? new()
            : new();
    }
}

public class InvestigatorListItem
{
    public Guid InvestigatorId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool HasKeypair { get; set; }
}

public class CategoryListItem
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class SettingItem
{
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AuditLogPage
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<AuditLogItem> Logs { get; set; } = new();
}

public class AuditLogItem
{
    public long LogId { get; set; }
    public byte ActorType { get; set; }
    public string? ActorId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? TargetEntity { get; set; }
    public string? TargetId { get; set; }
    public string? Detail { get; set; }
    public string? Ipaddress { get; set; }
    public DateTime Timestamp { get; set; }
}
