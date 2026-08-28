using FluentValidation;
using Kasko.Business.DTOs.Customer;

namespace Kasko.Business.Validators;
    public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
    {
        public CreateCustomerDtoValidator() { 
        RuleFor(x => x.FirstName)
       .NotEmpty()
       .WithMessage("İsim boş olamaz.")
       .MaximumLength(25) 
       .WithMessage("İsim en fazla 25 karakter olabilir.");
        
        RuleFor(x => x.LastName)
       .NotEmpty()
       .WithMessage("Soyisim boş olamaz.")
       .MaximumLength(25)
       .WithMessage("Soyisim en fazla 25 karakter olabilir.");


        RuleFor(x=> x.IdentityNumber)
       .NotEmpty()
       .WithMessage("Kimlik numarası boş olamaz.")
       .Length(11)
       .WithMessage("Kimlik numarası 11 karakter olmalıdır.")
       .Matches("^[0-9]+$")
       .WithMessage("Kimlik numarası sadece rakamlardan oluşmalıdır.");

        RuleFor(x => x.DateOfBirth)
       .NotEmpty()
       .WithMessage("Doğum tarihi boş olamaz.")
       .LessThan(DateTime.UtcNow)
       .WithMessage("Doğum tarihi bugünden küçük olmalıdır.");
            
        RuleFor(x => x.Email)
       .NotEmpty()
       .WithMessage("e-posta boş olamaz.")
       .EmailAddress()
       .WithMessage("Geçerli bir e-posta adresi giriniz.")
       .MaximumLength(150)
       .WithMessage("e-posta en fazla 150 karakter olabilir.");

        RuleFor(x => x.PhoneNumber)
       .NotEmpty()
       .WithMessage("Telefon boş olamaz.")
       .MaximumLength(15)
       .WithMessage("Telefon en fazla 15 karakter olabilir.")
       .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Geçerli bir telefon numarası giriniz.");

        RuleFor(x => x.Address)
       .NotEmpty()
       .WithMessage("Adres boş olamaz.")
       .MaximumLength(100)
       .WithMessage("Adres en fazla 100 karakter olabilir.");
            
        RuleFor(x => x.City)
       .NotEmpty()
       .WithMessage("Şehir boş olamaz.")
       .MaximumLength(50)
       .WithMessage("Şehir en fazla 50 karakter olabilir.");

        RuleFor(x => x.District)
       .NotEmpty()
       .WithMessage("İlçe boş olamaz.")
       .MaximumLength(50)
       .WithMessage("İlçe en fazla 50 karakter olabilir.");
            
       
        }
    }

