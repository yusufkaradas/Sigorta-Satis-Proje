using FluentValidation;
using Kasko.Business.DTOs.User;

namespace Kasko.Business.Validators
{
    public class UpdateUserDtoValidator
        : AbstractValidator<UpdateUserDto>
    {
        public UpdateUserDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Kullanıcı bilgisi zorunludur.");

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("Ad alanı boş olamaz.")
                .MaximumLength(50)
                .WithMessage("Ad alanı maximum 50 karakter içerebilir.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Soyad alanı boş olamaz.")
                .MaximumLength(50)
                .WithMessage("Soyad alanı maximum 50 karakter içerebilir.");

            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email alanı boş olamaz.")
                .EmailAddress()
                .WithMessage("Geçerli bir email adresi giriniz.");

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$")
                .WithMessage("Geçerli bir telefon numarası giriniz.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.RoleId)
                .NotEmpty()
                .WithMessage("Rol alanı boş olamaz.");
        }
    }
}