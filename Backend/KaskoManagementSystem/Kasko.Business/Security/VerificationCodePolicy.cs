namespace Kasko.Business.Security;

public class VerificationCodePolicy
{
    public VerificationCodePolicy(bool exposeCodes)
    {
        ExposeCodes = exposeCodes;
    }

    public bool ExposeCodes { get; }

    public string? Reveal(string code)
    {
        return ExposeCodes ? code : null;
    }
}
