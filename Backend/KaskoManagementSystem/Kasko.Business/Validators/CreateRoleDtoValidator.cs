using FluentValidation;
using Kasko.Business.DTOs.Role;




namespace Kasko.Business.Validators
{
    public class CreateRoleDtoValidator : AbstractValidator<CreateRoleDto>
    {

        public CreateRoleDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Rol adı boş olamaz.")
                .MinimumLength(5).WithMessage("Rol adı en az  karakter olmalıdır.")
                .MaximumLength(20).WithMessage("Rol adı en fazla  karakter olabilir.");
        }
    }
}
