using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Interfaces;

namespace WhistleblowerPlatform.Infrastructure.Services;

public class HCaptchaService : IHCaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HCaptchaService> _logger;
    private readonly string _secretKey;
    private readonly string _verifyUrl;

    public HCaptchaService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<HCaptchaService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _secretKey = _configuration["HCaptcha:SecretKey"]
            ?? throw new InvalidOperationException("HCaptcha:SecretKey not configured");
        _verifyUrl = _configuration["HCaptcha:VerifyUrl"]
            ?? "https://hcaptcha.com/siteverify";
    }

    public async Task<bool> VerifyTokenAsync(string token, string? userIp = null)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("hCaptcha verification called with empty token");
            return false;
        }

        try
        {
            // Build verification request
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _secretKey),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", userIp ?? string.Empty)
            });

            // Call hCaptcha API
            _logger.LogDebug("Calling hCaptcha verification API");
            var response = await _httpClient.PostAsync(_verifyUrl, formContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("hCaptcha API returned {StatusCode}", response.StatusCode);
                return false;
            }

            // Parse response
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var verificationResult = JsonSerializer.Deserialize<HCaptchaVerificationResponse>(
                jsonResponse,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (verificationResult == null)
            {
                _logger.LogError("Failed to deserialize hCaptcha response");
                return false;
            }

            // Log errors if verification failed
            if (!verificationResult.Success && verificationResult.ErrorCodes?.Length > 0)
            {
                _logger.LogWarning("hCaptcha verification failed: {Errors}",
                    string.Join(", ", verificationResult.ErrorCodes));
            }
            else if (verificationResult.Success)
            {
                _logger.LogInformation("hCaptcha verification successful");
            }

            return verificationResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during hCaptcha verification");
            return false;
        }
    }
}