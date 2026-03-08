using Microsoft.AspNetCore.Identity;
using WhistleblowerPlatform.Application.DTOs;
using WhistleblowerPlatform.Application.Interfaces;
using WhistleblowerPlatform.Domain.Entities;

namespace WhistleblowerPlatform.Application.UseCases;

public class LoginUseCase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IMfaService _mfaService;
    private readonly IInvestigatorRepository _investigatorRepository;

    public LoginUseCase(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IMfaService mfaService,
        IInvestigatorRepository investigatorRepository)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _mfaService = mfaService;
        _investigatorRepository = investigatorRepository;
    }

    public async Task<(LoginResponse Response, string? RawRefreshToken)> ExecuteAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return (Fail("Invalid credentials."), null);

        if (await _userManager.IsLockedOutAsync(user))
            return (Fail("Account is locked. Try again later."), null);

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            await _userManager.AccessFailedAsync(user);
            return (Fail("Invalid credentials."), null);
        }

        if (user.InvestigatorId.HasValue)
        {
            var investigator = await _investigatorRepository.GetByIdAsync(user.InvestigatorId.Value);

            if (investigator is not null && investigator.Mfaenabled)
            {
                if (string.IsNullOrEmpty(request.TotpCode))
                    return (new LoginResponse { Success = false, RequiresMfa = true }, null);

                if (!_mfaService.ValidateTotpCode(investigator.Mfasecret, request.TotpCode))
                    return (Fail("Invalid MFA code."), null);
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            if (investigator is not null)
                await _investigatorRepository.UpdateLastLoginAsync(investigator.InvestigatorId, DateTime.UtcNow);
        }
        else
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();
        await _tokenService.StoreRefreshTokenAsync(user, refreshToken);

        return (new LoginResponse
        {
            Success = true,
            AccessToken = accessToken
        }, refreshToken);
    }

    private static LoginResponse Fail(string error) =>
        new() { Success = false, Error = error };
}
