using System.Linq.Expressions;
using Kasko.Business.DTOs.Auth;
using Kasko.Business.Exceptions;
using Kasko.Business.Security;
using Kasko.Business.Services;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Kasko.Business.Tests;

public class AuthSecurityTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly Mock<IUserRepository> _users = new();

    private readonly Mock<IRoleRepository> _roles = new();

    private readonly PasswordHasherService _hasher = new(new PasswordHasher<User>());

    private readonly Role _customerRole = new() { Id = Guid.NewGuid(), Name = "Customer" };

    private readonly Role _adminRole = new() { Id = Guid.NewGuid(), Name = "Admin" };

    public AuthSecurityTests()
    {
        _unitOfWork.Setup(x => x.Users).Returns(_users.Object);
        _unitOfWork.Setup(x => x.Roles).Returns(_roles.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _roles.Setup(x => x.GetByIdAsync(_customerRole.Id)).ReturnsAsync(_customerRole);
        _roles.Setup(x => x.GetByIdAsync(_adminRole.Id)).ReturnsAsync(_adminRole);
    }

    private AuthService CreateService(bool exposeCodes)
    {
        return new AuthService(
            _unitOfWork.Object,
            _hasher,
            new JwtTokenService(Mock.Of<IConfiguration>()),
            new VerificationCodePolicy(exposeCodes));
    }

    private User AddUser(string email, Role role, string password = "Gizli123", bool isActive = true)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            RoleId = role.Id,
            IsActive = isActive,
            PhoneNumber = "5321234567"
        };

        user.PasswordHash = _hasher.HashPassword(user, password);

        _users
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((Expression<Func<User, bool>> predicate) =>
                new[] { user }.Where(predicate.Compile()).ToList());

        return user;
    }

    [Fact]
    public async Task SendResetCode_WhenCodesHidden_ShouldNotReturnCode()
    {
        AddUser("elif@ornek.com", _customerRole);

        var result = await CreateService(exposeCodes: false)
            .SendPasswordResetCodeAsync(new PasswordResetRequestDto { Email = "elif@ornek.com" });

        Assert.Null(result.DemoCode);
    }

    [Fact]
    public async Task SendResetCode_WhenDemoCodesEnabled_ShouldReturnCodeForCustomer()
    {
        AddUser("elif@ornek.com", _customerRole);

        var result = await CreateService(exposeCodes: true)
            .SendPasswordResetCodeAsync(new PasswordResetRequestDto { Email = "elif@ornek.com" });

        Assert.Matches("^[0-9]{6}$", result.DemoCode);
    }

    [Fact]
    public async Task SendResetCode_ForStaffAccount_ShouldNeverIssueCode()
    {
        AddUser("admin@ornek.com", _adminRole);

        var service = CreateService(exposeCodes: true);

        var result = await service.SendPasswordResetCodeAsync(new PasswordResetRequestDto { Email = "admin@ornek.com" });

        Assert.Null(result.DemoCode);

        await Assert.ThrowsAsync<BadRequestException>(() => service.ResetPasswordAsync(new PasswordResetConfirmDto
        {
            Email = "admin@ornek.com",
            Code = "123456",
            NewPassword = "YeniSifre1"
        }));
    }

    [Fact]
    public async Task SendResetCode_ForUnknownEmail_ShouldNotRevealAccountExistence()
    {
        AddUser("elif@ornek.com", _customerRole);

        var result = await CreateService(exposeCodes: false)
            .SendPasswordResetCodeAsync(new PasswordResetRequestDto { Email = "yok@ornek.com" });

        Assert.Null(result.DemoCode);
        Assert.Equal("kayıtlı telefonunuz", result.MaskedPhone);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ShouldFailIdentically()
    {
        AddUser("elif@ornek.com", _customerRole);

        var service = CreateService(exposeCodes: false);

        var unknown = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.LoginAsync(new LoginDto { Email = "yok@ornek.com", Password = "Gizli123" }));

        var wrongPassword = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.LoginAsync(new LoginDto { Email = "elif@ornek.com", Password = "Yanlis123" }));

        Assert.Equal(unknown.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task Login_InactiveAccountWithWrongPassword_ShouldNotRevealInactiveState()
    {
        AddUser("pasif@ornek.com", _customerRole, isActive: false);

        var error = await Assert.ThrowsAsync<BadRequestException>(() =>
            CreateService(exposeCodes: false).LoginAsync(new LoginDto { Email = "pasif@ornek.com", Password = "Yanlis123" }));

        Assert.Equal("E-posta veya şifre hatalı.", error.Message);
    }
}
