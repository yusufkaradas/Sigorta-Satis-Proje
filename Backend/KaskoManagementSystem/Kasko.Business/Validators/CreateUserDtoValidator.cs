using FluentValidation;
using Kasko.Business.DTOs.User;

namespace Kasko.Business.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        public CreateUserDtoValidator() {
        
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("Ad alanı boş olamaz.")
                .MaximumLength(50).WithMessage("Ad alanı maximum 50 karakter içerebilir.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Soyad alanı boş olamaz.")
                .MaximumLength(50).WithMessage("Soyad alanı maximum 50 karakter içerebilir.");
            
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş olamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.")
                .MaximumLength(150).WithMessage("Email alanı maximum 150 karakter içerebilir.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre alanı boş olamaz.")
                .MinimumLength(6).WithMessage("Şifre minimum 6 karakter içerebilir.")
                .MaximumLength(20).WithMessage("Şifre maximum 20 karakter içerebilir.")
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,20}$")
                .WithMessage("Şifre en az bir büyük harf, bir küçük harf, bir rakam ve bir özel karakter içermelidir.");
           
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Geçerli bir telefon numarası giriniz.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
            
            RuleFor(x => x.RoleId)
                .NotEmpty().WithMessage("Rol alanı boş olamaz.");
        }
    }
}
