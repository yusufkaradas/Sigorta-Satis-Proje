using System.Security.Claims;
using Kasko.Business.Exceptions;
using Kasko.Business.Security;
using Kasko.DataAccess.Repositories.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Manager")]
    public class AccountController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasherService _passwordHasherService;

        public AccountController(
            IUnitOfWork unitOfWork,
            PasswordHasherService passwordHasherService)
        {
            _unitOfWork = unitOfWork;
            _passwordHasherService = passwordHasherService;
        }

        public class UpdateAccountDto
        {
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; }
        }

        public class ChangePasswordDto
        {
            public string CurrentPassword { get; set; } = string.Empty;
            public string NewPassword { get; set; } = string.Empty;
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var user = await GetCurrentUserAsync();

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.PhoneNumber,
                Role = user.Role?.Name
            });
        }

        [HttpPut("me")]
        public async Task<IActionResult> Update([FromBody] UpdateAccountDto dto)
        {
            var user = await GetCurrentUserAsync();

            var firstName = dto.FirstName.Trim();
            var lastName = dto.LastName.Trim();
            var email = dto.Email.Trim().ToLowerInvariant();
            var phone = new string((dto.PhoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());

            if (firstName.Length < 2 || lastName.Length < 2)
            {
                throw new BadRequestException("Ad ve soyad en az 2 karakter olmalıdır.");
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                throw new BadRequestException("Geçerli bir e-posta adresi girin.");
            }

            if (phone.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^5\d{9}$"))
            {
                throw new BadRequestException("Telefon numarası 5 ile başlayan 10 haneli olmalıdır.");
            }

            var emailTaken =
                (await _unitOfWork.Users.FindAsync(x => x.Email == email && x.Id != user.Id && !x.IsDeleted))
                .Any();

            if (emailTaken)
            {
                throw new BadRequestException("Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
            }

            user.FirstName = firstName.ToUpperInvariant();
            user.LastName = lastName.ToUpperInvariant();
            user.Email = email;
            user.PhoneNumber = phone.Length > 0 ? phone : null;

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await GetCurrentUserAsync();

            if (!_passwordHasherService.VerifyPassword(user, dto.CurrentPassword, user.PasswordHash))
            {
                throw new BadRequestException("Mevcut şifre hatalı.");
            }

            if (dto.NewPassword.Length < 8 ||
                !dto.NewPassword.Any(char.IsUpper) ||
                !dto.NewPassword.Any(char.IsLower) ||
                !dto.NewPassword.Any(char.IsDigit))
            {
                throw new BadRequestException("Yeni şifre en az 8 karakter olmalı; büyük harf, küçük harf ve rakam içermelidir.");
            }

            user.PasswordHash = _passwordHasherService.HashPassword(user, dto.NewPassword);

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        private async Task<Kasko.Entities.Concrete.User> GetCurrentUserAsync()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!Guid.TryParse(value, out var userId))
            {
                throw new UnauthorizedAccessException();
            }

            return await _unitOfWork.Users.GetByIdWithRoleAsync(userId)
                ?? throw new NotFoundException("Kullanıcı bulunamadı.");
        }
    }
}
