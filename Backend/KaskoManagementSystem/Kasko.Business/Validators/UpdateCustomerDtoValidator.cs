using FluentValidation;
using Kasko.Business.DTOs.Customer;

namespace Kasko.Business.Validators;

public class UpdateCustomerDtoValidator
    : AbstractValidator<UpdateCustomerDto>
{
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Müşteri Id alanı boş olamaz.");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("Ad alanı boş olamaz.")
            .MaximumLength(25)
            .WithMessage("Ad en fazla 25 karakter olabilir.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Soyad alanı boş olamaz.")
            .MaximumLength(25)
            .WithMessage("Soyad en fazla 25 karakter olabilir.");

        RuleFor(x => x.IdentityNumber)
            .NotEmpty()
            .WithMessage("TC Kimlik No boş olamaz.")
            .Length(11)
            .WithMessage("TC Kimlik No 11 haneli olmalıdır.")
            .Matches("^[0-9]+$")
            .WithMessage("TC Kimlik No sadece rakamlardan oluşmalıdır.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty()
            .WithMessage("Doğum tarihi boş olamaz.")
            .LessThan(DateTime.UtcNow.Date)
            .WithMessage("Doğum tarihi gelecekte olamaz.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("E-posta alanı boş olamaz.")
            .EmailAddress()
            .WithMessage("Geçerli bir e-posta adresi giriniz.")
            .MaximumLength(150)
            .WithMessage("E-posta en fazla 150 karakter olabilir.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(15)
            .WithMessage("Telefon numarası en fazla 15 karakter olabilir.")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber));

        RuleFor(x => x.Address)
            .NotEmpty()
            .WithMessage("Adres alanı boş olamaz.")
            .MaximumLength(100)
            .WithMessage("Adres en fazla 100 karakter olabilir.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("Şehir alanı boş olamaz.")
            .MaximumLength(50)
            .WithMessage("Şehir en fazla 50 karakter olabilir.");

        RuleFor(x => x.District)
            .NotEmpty()
            .WithMessage("İlçe alanı boş olamaz.")
            .MaximumLength(50)
            .WithMessage("İlçe en fazla 50 karakter olabilir.");
    }
}