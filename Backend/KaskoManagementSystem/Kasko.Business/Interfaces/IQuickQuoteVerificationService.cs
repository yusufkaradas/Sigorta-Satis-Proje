namespace Kasko.Business.Interfaces;

public interface IQuickQuoteVerificationService
{
    string SendCode(string identityNumber, string phoneNumber);

    string VerifyCode(string identityNumber, string phoneNumber, string code);

    void EnsureVerified(string? token, string identityNumber, string phoneNumber);
}
