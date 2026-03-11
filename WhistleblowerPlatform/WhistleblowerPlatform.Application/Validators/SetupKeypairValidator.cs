using WhistleblowerPlatform.Application.DTOs;

namespace WhistleblowerPlatform.Application.Validators;

public static class SetupKeypairValidator
{
    public static List<string> Validate(SetupKeypairRequest r)
    {
        var errors = new List<string>();

        if (r.PublicKey.Length < 100)
            errors.Add("PublicKey appears invalid");

        if (r.EncryptedPrivateKey.Length < 100)
            errors.Add("EncryptedPrivateKey appears invalid");

        if (r.PrivateKeySalt.Length != 16)
            errors.Add("PrivateKeySalt must be 16 bytes");

        if (r.PrivateKeyIv.Length != 12)
            errors.Add("PrivateKeyIv must be 12 bytes");

        return errors;
    }
}
