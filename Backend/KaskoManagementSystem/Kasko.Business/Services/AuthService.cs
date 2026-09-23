using Kasko.Business.DTOs.Auth;
using Kasko.Business.Interfaces;
using Kasko.Business.Security;
using Kasko.Business.Exceptions;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Kasko.Business.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasherService _passwordHasherService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthService(
        IUnitOfWork unitOfWork,
        PasswordHasherService passwordHasherService,
        JwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasherService = passwordHasherService;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var loginEmail = (dto.Email ?? string.Empty).Trim().ToLower();

        var users = await _unitOfWork.Users
            .FindAsync(x => x.Email.ToLower() == loginEmail && !x.IsDeleted);

        var user = users.FirstOrDefault();

        if (user == null)
        {
            throw new NotFoundException(
                "E-posta veya şifre hatalı.");
        }

        if (!user.IsActive)
        {
            throw new BadRequestException(
                "Kullanıcı aktif değil.");
        }

        var passwordValid =
            _passwordHasherService.VerifyPassword(
                user,
                dto.Password,
                user.PasswordHash);

        if (!passwordValid)
        {
            throw new BadRequestException(
                "E-posta veya şifre hatalı.");
        }

        var role =
            await _unitOfWork.Roles
                .GetByIdAsync(user.RoleId);

        if (role == null)
        {
            throw new BadRequestException(
                "Hesabınıza bir rol atanmamış. Lütfen yöneticinizle iletişime geçin.");
        }

        var tokenResult =
            _jwtTokenService.GenerateToken(
                user,
                role.Name);

        return new LoginResponseDto
        {
            Token = tokenResult.Token,
            Expiration = tokenResult.Expiration
        };
    }

    public async Task<Guid> RegisterAsync(
        RegisterDto dto)
    {
        var identityNumber =
            new string(
                dto.IdentityNumber
                    .Where(char.IsDigit)
                    .ToArray());

        if (
            identityNumber.Length != 11 ||
            identityNumber.All(x => x == '0'))
        {
            throw new BadRequestException(
                "Geçerli bir T.C. Kimlik No giriniz.");
        }

        var emailExists =
            await _unitOfWork.Users
                .AnyAsync(x => x.Email == dto.Email);

        if (emailExists)
        {
            throw new BadRequestException(
                "Bu e-posta adresi zaten kayıtlı.");
        }

        var identityExists =
            await _unitOfWork.Customers
                .AnyAsync(
                    x => x.IdentityNumber == identityNumber);

        if (identityExists)
        {
            throw new BadRequestException(
                "Bu T.C. Kimlik No ile kayıtlı bir müşteri zaten mevcut.");
        }

        var roles =
            await _unitOfWork.Roles
                .FindAsync(
                    x => x.Name == "Customer");

        var role =
            roles.FirstOrDefault();

        if (role == null)
        {
            throw new NotFoundException(
                "Customer rolü bulunamadı.");
        }

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdentityNumber = identityNumber,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            City = dto.City,
            District = dto.District,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Customers
            .AddAsync(customer);

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            RoleId = role.Id,
            CustomerId = customer.Id,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHasherService
                .HashPassword(
                    user,
                    dto.Password);

        await _unitOfWork.Users
            .AddAsync(user);

        await Notifications.NotificationWriter.ToCustomerAsync(
            _unitOfWork,
            customer.Id,
            "ACCOUNT_CREATED",
            "NetSigorta'ya hoş geldiniz",
            "Hesabınız oluşturuldu. Araçlarınızı ekleyip birkaç adımda kasko teklifi alabilirsiniz.",
            customer.Id);

        await Notifications.NotificationWriter.ToRolesAsync(
            _unitOfWork,
            Notifications.NotificationWriter.Staff,
            "CUSTOMER_REGISTERED",
            "Yeni müşteri kaydı",
            $"{customer.FirstName} {customer.LastName} müşteri portalı üzerinden kayıt oldu.",
            customer.Id);

        await _unitOfWork
            .SaveChangesAsync();

        return customer.Id;
    }

    private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt, int Attempts)> ResetCodes = new();

    public async Task<PasswordResetCodeResponseDto> SendPasswordResetCodeAsync(PasswordResetRequestDto dto)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        var user = (await _unitOfWork.Users.FindAsync(x => x.Email.ToLower() == email && !x.IsDeleted)).FirstOrDefault();

        if (user == null || !user.IsActive)
        {
            throw new NotFoundException("Bu e-posta adresiyle kayıtlı aktif bir hesap bulunamadı.");
        }

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        ResetCodes[email] = (code, DateTime.UtcNow.AddMinutes(5), 0);

        var digits = new string((user.PhoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());

        var masked = digits.Length >= 4 ? $"+90 5** *** ** {digits[^2..]}" : "kayıtlı telefonunuz";

        return new PasswordResetCodeResponseDto
        {
            MaskedPhone = masked,
            DemoCode = code
        };
    }

    public async Task ResetPasswordAsync(PasswordResetConfirmDto dto)
    {
        var email = (dto.Email ?? string.Empty).Trim().ToLowerInvariant();

        if (!ResetCodes.TryGetValue(email, out var pending))
        {
            throw new BadRequestException("Doğrulama kodu bulunamadı. Lütfen yeni kod isteyin.");
        }

        if (pending.ExpiresAt < DateTime.UtcNow)
        {
            ResetCodes.TryRemove(email, out _);
            throw new BadRequestException("Doğrulama kodunun süresi doldu. Lütfen yeni kod isteyin.");
        }

        if (pending.Attempts >= 5)
        {
            ResetCodes.TryRemove(email, out _);
            throw new BadRequestException("Çok fazla hatalı deneme yapıldı. Lütfen yeni kod isteyin.");
        }

        if (!string.Equals(pending.Code, dto.Code?.Trim(), StringComparison.Ordinal))
        {
            ResetCodes[email] = pending with { Attempts = pending.Attempts + 1 };
            throw new BadRequestException("Doğrulama kodu hatalı.");
        }

        var password = dto.NewPassword ?? string.Empty;

        if (password.Length < 8 || password.Length > 20 ||
            !Regex.IsMatch(password, "[A-Z]") ||
            !Regex.IsMatch(password, "[a-z]") ||
            !Regex.IsMatch(password, "[0-9]"))
        {
            throw new BadRequestException("Şifre 8-20 karakter olmalı; en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.");
        }

        var user = (await _unitOfWork.Users.FindAsync(x => x.Email.ToLower() == email && !x.IsDeleted)).FirstOrDefault();

        if (user == null)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        user.PasswordHash = _passwordHasherService.HashPassword(user, password);
        user.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        ResetCodes.TryRemove(email, out _);
    }
}
