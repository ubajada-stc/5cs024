using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WhistleblowerPlatform.Application.DTOs;

namespace WhistleblowerPlatform.Application.Validators;

public static class SubmitReportValidator
{
    public static List<string> Validate(SubmitReportRequest request)
    {
        var errors = new List<string>();

        if (request.TokenHash.Length != 32)
        {
            errors.Add("TokenHash must be exactly 32 bytes");
        }

        if (request.WbKeySalt.Length != 16)
        {
            errors.Add("WbKeySalt must be exactly 16 bytes");
        }

        if (request.EncryptedContent.Length == 0)
        {
            errors.Add("EncryptedContent is required");
        }

        if (request.EncryptedKeyEnvelope.Length == 0)
        {
            errors.Add("EncryptedKeyEnvelope is required");

        }

        if (request.WbKeyEnvelope.Length == 0)
        {
            errors.Add("WbKeyEnvelope is required");
        }

        if (request.WbPublicKey.Length == 0)
        {
            errors.Add("WbPublicKey is required");
        }

        if (request.EncryptedWbPrivateKey.Length == 0)
        {
            errors.Add("EncryptedWbPrivateKey is required");
        }

        if (request.SelfIdentified)
        {
            if (request.EncryptedIdentity == null || request.EncryptedIdentity.Length == 0)
            {
                errors.Add("EncryptedIdentity is required when SelfIdentified is true");
            }

            if (request.EncryptedIdentityKeyEnvelope == null || request.EncryptedIdentityKeyEnvelope.Length == 0)
            {
                errors.Add("EncryptedIdentityKeyEnvelope is required when SelfIdentified is true.");
            }
        }

        if (string.IsNullOrWhiteSpace(request.HCaptchaToken))
        {
            errors.Add("CAPTCHA verification is required");
        }

        return errors;
    }
}
