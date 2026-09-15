using Kasko.Business.DTOs.Auth;
using Kasko.Business.Interfaces;
using Kasko.Business.Security;
using Kasko.Business.Exceptions;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

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
        var users = await _unitOfWork.Users
            .FindAsync(x => x.Email == dto.Email);

        var user = users.FirstOrDefault();

        if (user == null)
        {
            throw new NotFoundException(
                "Email veya şifre hatalı.");
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
                "Email veya şifre hatalı.");
        }

        var role =
            await _unitOfWork.Roles
                .GetByIdAsync(user.RoleId);

        if (role == null)
        {
            throw new Exception(
                "Kullanıcı rolü bulunamadı.");
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
                "Bu email adresi zaten kayıtlı.");
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

        await _unitOfWork
            .SaveChangesAsync();

        return customer.Id;
    }
}