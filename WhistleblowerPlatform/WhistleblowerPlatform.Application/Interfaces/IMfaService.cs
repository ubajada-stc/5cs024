namespace WhistleblowerPlatform.Application.Interfaces;

public interface IMfaService
{
    string GenerateMfaSecret();
    string GenerateQrCodeUri(string email, string secret);
    bool ValidateTotpCode(string secret, string code);
}
