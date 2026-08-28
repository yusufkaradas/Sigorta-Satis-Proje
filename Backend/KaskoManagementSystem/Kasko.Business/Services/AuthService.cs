using Kasko.Business.DTOs.Auth;
using Kasko.Business.Interfaces;
using Kasko.Business.Security;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Business.Exceptions;

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
        var role = await _unitOfWork.Roles.GetByIdAsync(user.RoleId);

        if (role == null)
        {
            throw new Exception("Kullanıcı rolü bulunamadı.");
        }

            var tokenResult =
            _jwtTokenService.GenerateToken(user, role.Name);

        return new LoginResponseDto
        {
            Token = tokenResult.Token,
            Expiration = tokenResult.Expiration
        };
    }
}