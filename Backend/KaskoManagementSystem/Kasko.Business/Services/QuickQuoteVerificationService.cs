using System.Collections.Concurrent;
using System.Security.Cryptography;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;

namespace Kasko.Business.Services;

public class QuickQuoteVerificationService : IQuickQuoteVerificationService
{
    private static readonly ConcurrentDictionary<string, PendingCode> Codes = new();

    private static readonly ConcurrentDictionary<string, VerifiedSession> Sessions = new();

    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(3);

    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(30);

    private const int MaxAttempts = 5;

    public string SendCode(string identityNumber, string phoneNumber)
    {
        var key = BuildKey(identityNumber, phoneNumber);

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        Codes[key] = new PendingCode(code, DateTime.UtcNow.Add(CodeLifetime), 0);

        return code;
    }

    public string VerifyCode(string identityNumber, string phoneNumber, string code)
    {
        var key = BuildKey(identityNumber, phoneNumber);

        if (!Codes.TryGetValue(key, out var pending))
        {
            throw new BadRequestException("Doğrulama kodu bulunamadı. Lütfen yeni kod isteyin.");
        }

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            Codes.TryRemove(key, out _);

            throw new BadRequestException("Doğrulama kodunun süresi doldu. Lütfen yeni kod isteyin.");
        }

        if (pending.Attempts >= MaxAttempts)
        {
            Codes.TryRemove(key, out _);

            throw new BadRequestException("Çok fazla hatalı deneme yapıldı. Lütfen yeni kod isteyin.");
        }

        if (!string.Equals(pending.Code, code?.Trim(), StringComparison.Ordinal))
        {
            Codes[key] = pending with { Attempts = pending.Attempts + 1 };

            throw new BadRequestException("Doğrulama kodu hatalı.");
        }

        Codes.TryRemove(key, out _);

        var token = Guid.NewGuid().ToString("N");

        Sessions[token] = new VerifiedSession(key, DateTime.UtcNow.Add(SessionLifetime));

        return token;
    }

    public void EnsureVerified(string? token, string identityNumber, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            !Sessions.TryGetValue(token, out var session))
        {
            throw new BadRequestException("Bu işlem için telefon doğrulaması gerekiyor.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            Sessions.TryRemove(token, out _);

            throw new BadRequestException("Doğrulama süreniz doldu. Lütfen tekrar doğrulayın.");
        }

        if (session.Key != BuildKey(identityNumber, phoneNumber))
        {
            throw new BadRequestException("Doğrulama bilgileri bu işlemle eşleşmiyor.");
        }
    }

    private static string BuildKey(string identityNumber, string phoneNumber)
    {
        return $"{identityNumber?.Trim()}|{NormalizePhone(phoneNumber)}";
    }

    private static string NormalizePhone(string? phoneNumber)
    {
        var digits = new string((phoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length == 12 && digits.StartsWith("90"))
        {
            digits = digits[2..];
        }

        if (digits.Length == 11 && digits.StartsWith("0"))
        {
            digits = digits[1..];
        }

        return digits;
    }

    private sealed record PendingCode(string Code, DateTime ExpiresAt, int Attempts);

    private sealed record VerifiedSession(string Key, DateTime ExpiresAt);
}
