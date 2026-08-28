using FluentValidation;
using Kasko.Business.DTOs.Quote;

namespace Kasko.Business.Validators.Quote
{
    public class UpdateQuoteDtoValidator : AbstractValidator<UpdateQuoteDto>
    {
        public UpdateQuoteDtoValidator()
        {
            RuleFor(x => x.ValidUntil)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Teklif geçerlilik tarihi gelecekte olmalıdır.");
        }
    }
}