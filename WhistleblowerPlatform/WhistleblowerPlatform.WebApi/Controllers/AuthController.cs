using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Application.UseCases;
using WhistleblowerPlatform.Domain.Entities;
using WhistleblowerPlatform.Infrastructure.Persistence;
using WhistleblowerPlatform.Infrastructure.Services;

namespace WhistleblowerPlatform.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string RefreshTokenCookie = "wb_refresh";
    private const string UserIdCookie = "wb_user_id";

    private readonly LoginUseCase _loginUseCase;
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly WhistleblowerDbContext _dbContext;

    public AuthController(
        LoginUseCase loginUseCase,
        ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        JwtSettings jwtSettings,
        WhistleblowerDbContext dbContext)
    {
        _loginUseCase = loginUseCase;
        _tokenService = tokenService;
        _userManager = userManager;
        _jwtSettings = jwtSettings;
        _dbContext = dbContext;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (response, rawRefreshToken) = await _loginUseCase.ExecuteAsync(request);

        if (!response.Success)
        {
            if (!response.RequiresMfa)
            {
                var failedUser = await _userManager.FindByEmailAsync(request.Email);
                await WriteAuditLogAsync(0, failedUser?.Id.ToString(), "LoginFailed", "AspNetUsers", request.Email);
            }
            return response.RequiresMfa ? Ok(response) : Unauthorized(response);
        }

        response.ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60;

        var user = await _userManager.FindByEmailAsync(request.Email);
        SetRefreshCookies(rawRefreshToken!, user!.Id);

        var actorType = response.Role == "Admin" ? (byte)2 : (byte)1;
        await WriteAuditLogAsync(actorType, user.Id.ToString(), "Login", "AspNetUsers", user.Id.ToString());

        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookie, out var rawToken) ||
            !Request.Cookies.TryGetValue(UserIdCookie, out var userIdStr) ||
            !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null) return Unauthorized();

        if (!await _tokenService.ValidateRefreshTokenAsync(user, rawToken))
            return Unauthorized();

        await _dbContext.Entry(user).Reference(u => u.Admin).LoadAsync();
        await _dbContext.Entry(user).Reference(u => u.Investigator).LoadAsync();
        var role = user.AdminId.HasValue ? "Admin" : "Investigator";

        var newAccessToken = _tokenService.GenerateAccessToken(user, role);
        var newRefreshToken = _tokenService.GenerateRefreshToken();
        await _tokenService.StoreRefreshTokenAsync(user, newRefreshToken);

        SetRefreshCookies(newRefreshToken, user.Id);

        return Ok(new LoginResponse
        {
            Success = true,
            AccessToken = newAccessToken,
            ExpiresIn = _jwtSettings.AccessTokenExpiryMinutes * 60
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Cookies.TryGetValue(UserIdCookie, out var userIdStr) &&
            Guid.TryParse(userIdStr, out var userId))
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is not null)
            {
                await _tokenService.RevokeRefreshTokenAsync(user);
                await _dbContext.Entry(user).Reference(u => u.Admin).LoadAsync();
                var actorType = user.AdminId.HasValue ? (byte)2 : (byte)1;
                await WriteAuditLogAsync(actorType, user.Id.ToString(), "Logout", "AspNetUsers", user.Id.ToString());
            }
        }

        ClearRefreshCookies();
        return NoContent();
    }

    // DEV ONLY — remove before production
    [HttpPost("dev/link-admin")]
    public async Task<IActionResult> LinkIdentityToExistingAdmin([FromBody] LinkIdentityRequest request)
    {
        var admin = await _dbContext.Admins
            .FirstOrDefaultAsync(a => a.Email == request.Email);

        if (admin is null)
            return NotFound("No Admin found with that email.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Conflict("An Identity user already exists for that email.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            AdminId = admin.AdminId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Identity user created and linked to existing admin.", userId = user.Id });
    }

    // DEV ONLY — remove before production
    [HttpPost("dev/link-identity")]
    public async Task<IActionResult> LinkIdentityToExistingInvestigator([FromBody] LinkIdentityRequest request)
    {
        var investigator = await _dbContext.Investigators
            .FirstOrDefaultAsync(i => i.Email == request.Email);

        if (investigator is null)
            return NotFound("No Investigator found with that email.");

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            return Conflict("An Identity user already exists for that email.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            InvestigatorId = investigator.InvestigatorId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok(new { message = "Identity user created and linked to existing investigator.", userId = user.Id });
    }

    private async Task WriteAuditLogAsync(byte actorType, string? actorId, string action, string targetEntity, string? targetId)
    {
        _dbContext.AuditLogs.Add(new WhistleblowerPlatform.Domain.Entities.AuditLog
        {
            ActorType = actorType,
            ActorId = actorId,
            Action = action,
            TargetEntity = targetEntity,
            TargetId = targetId,
            Ipaddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Request.Headers.UserAgent.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync();
    }

    private void SetRefreshCookies(string rawRefreshToken, Guid userId)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiryHours)
        };

        Response.Cookies.Append(RefreshTokenCookie, rawRefreshToken, cookieOptions);
        Response.Cookies.Append(UserIdCookie, userId.ToString(), cookieOptions);
    }

    private void ClearRefreshCookies()
    {
        var expired = new CookieOptions { Expires = DateTimeOffset.UnixEpoch };
        Response.Cookies.Append(RefreshTokenCookie, string.Empty, expired);
        Response.Cookies.Append(UserIdCookie, string.Empty, expired);
    }
}

public class LinkIdentityRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
