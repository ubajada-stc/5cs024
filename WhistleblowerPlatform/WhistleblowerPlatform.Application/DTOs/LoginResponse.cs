namespace WhistleblowerPlatform.Application.DTOs;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? AccessToken { get; set; }
    public int ExpiresIn { get; set; }
    public bool RequiresMfa { get; set; }
    public string? Error { get; set; }
    public string? Role { get; set; }
}
