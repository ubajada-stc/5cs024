using OtpNet;
using WhistleblowerPlatform.Application.Interfaces;

namespace WhistleblowerPlatform.Infrastructure.Services;

public class MfaService : IMfaService
{
    public string GenerateMfaSecret()
    {
        return Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
    }

    public string GenerateQrCodeUri(string email, string secret)
    {
        return $"otpauth://totp/WhistleblowerPlatform:{Uri.EscapeDataString(email)}" +
               $"?secret={secret}&issuer=WhistleblowerPlatform";
    }

    public bool ValidateTotpCode(string secret, string code)
    {
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        return totp.VerifyTotp(code, out _, VerificationWindow.RfcSpecifiedNetworkDelay);
    }
}
