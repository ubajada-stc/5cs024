using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhistleblowerPlatform.Application.Interfaces;

public interface IHCaptchaService
{
    /// <summary>
    /// Checks hCaptcha token with hCaptcha's API
    /// </summary>
    /// <param name="token">Token received from the frontend</param>
    /// <param name="userIp">Optional: User's IP address for additional validation</param>
    /// <returns>True if token is valid and verified, false otherwise</returns>
    Task<bool> VerifyTokenAsync(string token, string? userIp = null);
}
