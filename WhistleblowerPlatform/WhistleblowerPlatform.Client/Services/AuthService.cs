using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace WhistleblowerPlatform.Client.Services;

public class AuthService
{
    private readonly HttpClient _http;
    public string? AccessToken { get; private set; }
    public string? Role { get; private set; }

    public AuthService(HttpClient http) => _http = http;

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        using var response = await _http.PostAsJsonAsync("/api/auth/login", request);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>()
            ?? new LoginResponse { Success = false, Error = "Empty response" };
        if (result.Success)
        {
            AccessToken = result.AccessToken;
            Role = result.Role;
        }
        return result;
    }

    public async Task<bool> HasKeypairAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/investigator/keypair-status");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return false;
        var status = await resp.Content.ReadFromJsonAsync<KeypairStatusResponse>();
        return status?.HasKeypair ?? false;
    }

    public async Task<EncryptedKeyResponse?> GetEncryptedKeyAsync()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/investigator/encrypted-key");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode
            ? await resp.Content.ReadFromJsonAsync<EncryptedKeyResponse>()
            : null;
    }

    public async Task<bool> SetupKeypairAsync(SetupKeypairRequest request)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/investigator/setup-keypair");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        req.Content = JsonContent.Create(request);
        using var resp = await _http.SendAsync(req);
        return resp.IsSuccessStatusCode;
    }

    public async Task LogoutAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
            if (AccessToken != null)
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            await _http.SendAsync(req);
        }
        catch { }
        finally
        {
            AccessToken = null;
            Role = null;
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? TotpCode { get; set; }
}

public class LoginResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public bool RequiresMfa { get; set; }
    public string? Error { get; set; }
    public string? Role { get; set; }
}

public class SetupKeypairRequest
{
    public byte[] PublicKey { get; set; } = [];
    public byte[] EncryptedPrivateKey { get; set; } = [];
    public byte[] PrivateKeySalt { get; set; } = [];
    public byte[] PrivateKeyIv { get; set; } = [];
}

public class KeypairStatusResponse
{
    public bool HasKeypair { get; set; }
}

public class EncryptedKeyResponse
{
    public byte[] EncryptedPrivateKey { get; set; } = [];
    public byte[] Salt { get; set; } = [];
    public byte[] Iv { get; set; } = [];
    public byte[]? PublicKey { get; set; }
}
