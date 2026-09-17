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
        
            When(x => !string.IsNullOrWhiteSpace(x.IdentityNumber), () =>
            {
                RuleFor(x => x.IdentityNumber)
                    .Matches(@"^[1-9][0-9]{10}$").WithMessage("T.C. Kimlik No 11 haneli olmalı ve 0 ile başlamamalıdır.");

                RuleFor(x => x.DateOfBirth)
                    .NotNull().WithMessage("Doğum tarihi boş olamaz.")
                    .Must(date => date <= DateTime.UtcNow.Date.AddYears(-18)).WithMessage("Müşteri 18 yaşından büyük olmalıdır.")
                    .Must(date => date >= DateTime.UtcNow.Date.AddYears(-100)).WithMessage("Geçerli bir doğum tarihi giriniz.");

                RuleFor(x => x.PhoneNumber)
                    .NotEmpty().WithMessage("Müşteri için telefon numarası zorunludur.")
                    .Matches(@"^(\+90|0)?5[0-9]{9}$").WithMessage("Telefon numarası 5 ile başlayan 10 haneli olmalıdır.");

                RuleFor(x => x.City)
                    .NotEmpty().WithMessage("İl boş olamaz.")
                    .Length(2, 50).WithMessage("İl 2-50 karakter olmalıdır.");

                RuleFor(x => x.District)
                    .NotEmpty().WithMessage("İlçe boş olamaz.")
                    .Length(2, 50).WithMessage("İlçe 2-50 karakter olmalıdır.");

                RuleFor(x => x.Address)
                    .NotEmpty().WithMessage("Adres boş olamaz.")
                    .Length(10, 100).WithMessage("Adres 10-100 karakter olmalıdır.");
            });
        }
    }
}
