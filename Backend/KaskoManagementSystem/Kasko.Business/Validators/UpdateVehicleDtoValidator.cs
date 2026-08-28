using FluentValidation;
using Kasko.Business.DTOs.Vehicle;

namespace Kasko.Business.Validators
{
    public class UpdateVehicleDtoValidator : AbstractValidator<UpdateVehicleDto>
    {
        public UpdateVehicleDtoValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Müşteri bilgisi zorunludur.");

            RuleFor(x => x.PlateNumber)
                .NotEmpty()
                .WithMessage("Plaka boş olamaz.")
                .MaximumLength(20)
                .WithMessage("Plaka en fazla 20 karakter olabilir.");

            RuleFor(x => x.VIN)
                .NotEmpty()
                .WithMessage("VIN boş olamaz.")
                .Length(17)
                .WithMessage("VIN 17 karakter olmalıdır.");

            RuleFor(x => x.Brand)
                .NotEmpty()
                .WithMessage("Marka boş olamaz.")
                .MaximumLength(50)
                .WithMessage("Marka en fazla 50 karakter olabilir.");

            RuleFor(x => x.Model)
                .NotEmpty()
                .WithMessage("Model boş olamaz.")
                .MaximumLength(50)
                .WithMessage("Model en fazla 50 karakter olabilir.");

            RuleFor(x => x.ModelYear)
                .InclusiveBetween(1900, DateTime.UtcNow.Year + 1)
                .WithMessage("Geçerli bir model yılı giriniz.");

            RuleFor(x => x.EngineVolume)
                .GreaterThan(0)
                .When(x => x.EngineVolume.HasValue)
                .WithMessage("Motor hacmi 0'dan büyük olmalıdır.");

            RuleFor(x => x.EnginePower)
                .GreaterThan(0)
                .When(x => x.EnginePower.HasValue)
                .WithMessage("Motor gücü 0'dan büyük olmalıdır.");

            RuleFor(x => x.VehicleType)
                .IsInEnum()
                .WithMessage("Geçerli bir araç tipi seçiniz.");
            RuleFor(x => x.FuelType)
                .IsInEnum()
                .WithMessage("Geçerli bir yakıt tipi seçiniz.");
            RuleFor(x => x.TransmissionType).IsInEnum()
                .WithMessage("Geçerli bir şanzıman tipi seçiniz.");

            RuleFor(x => x.Color)
                .NotEmpty()
                .WithMessage("Renk boş olamaz.")
                .MaximumLength(30)
                .WithMessage("Renk en fazla 30 karakter olabilir.");
        }
    }
}