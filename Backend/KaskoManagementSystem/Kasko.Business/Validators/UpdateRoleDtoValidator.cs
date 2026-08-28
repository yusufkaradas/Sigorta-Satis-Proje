using FluentValidation;
using Kasko.Business.DTOs.Role;

namespace Kasko.Business.Validators
{
    public class UpdateRoleDtoValidator
        : AbstractValidator<UpdateRoleDto>
    {
        public UpdateRoleDtoValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty()
                .WithMessage("Rol bilgisi zorunludur.");

            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Rol adı boş olamaz.")
                .MaximumLength(50)
                .WithMessage("Rol adı en fazla 50 karakter olabilir.");
        }
    }
}