using FluentValidation;
using Kasko.Business.DTOs.Auth;

namespace Kasko.Business.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    private const string NamePattern = @"^[A-Za-zÇĞİÖŞÜçğıöşü]+( [A-Za-zÇĞİÖŞÜçğıöşü]+)*$";

    public RegisterDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad boş olamaz.")
            .Length(2, 25).WithMessage("Ad 2-25 karakter olmalıdır.")
            .Matches(NamePattern).WithMessage("Ad sadece harflerden oluşmalıdır.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad boş olamaz.")
            .Length(2, 25).WithMessage("Soyad 2-25 karakter olmalıdır.")
            .Matches(NamePattern).WithMessage("Soyad sadece harflerden oluşmalıdır.");

        RuleFor(x => x.IdentityNumber)
            .Matches(@"^[1-9][0-9]{10}$").WithMessage("T.C. Kimlik No 11 haneli olmalı ve 0 ile başlamamalıdır.");

        RuleFor(x => x.DateOfBirth)
            .Must(date => date <= DateTime.UtcNow.Date.AddYears(-18)).WithMessage("Kayıt olmak için 18 yaşından büyük olmalısınız.")
            .Must(date => date >= DateTime.UtcNow.Date.AddYears(-100)).WithMessage("Geçerli bir doğum tarihi giriniz.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.");

        RuleFor(x => x.PhoneNumber)
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

        RuleFor(x => x.Password)
            .Length(8, 20).WithMessage("Şifre 8-20 karakter olmalıdır.")
            .Matches(@"[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches(@"[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches(@"[0-9]").WithMessage("Şifre en az bir rakam içermelidir.");
    }
}
