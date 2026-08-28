using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Identity;

namespace Kasko.Business.Security;

public class PasswordHasherService
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public PasswordHasherService(
        IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public string HashPassword(
        User user,
        string password)
    {
        return _passwordHasher.HashPassword(
            user,
            password);
    }

    public bool VerifyPassword(
        User user,
        string password,
        string passwordHash)
    {
        var result =
            _passwordHasher.VerifyHashedPassword(
                user,
                passwordHash,
                password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}