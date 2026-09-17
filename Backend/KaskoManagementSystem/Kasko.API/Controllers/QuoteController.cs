using Kasko.Business.Interfaces;
using Kasko.Business.DTOs.Quote;
using Kasko.Business.Services.Abstract;
using Kasko.Business.Services;
using Kasko.Entities.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class QuoteController : ControllerBase
    {
        private readonly IQuoteService _quoteService;

        public QuoteController(IQuoteService quoteService)
        {
            _quoteService = quoteService;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var quotes = await _quoteService.GetAllAsync();

            return Ok(quotes);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var quote = await _quoteService.GetByIdAsync(id);

            if (quote == null)
            {

                {
                    return NotFound();
                }

            }
            return Ok(quote);
        }

        [HttpPost("calculate")]
        [Authorize(Roles = "Admin,Manager,Customer")]
        public async Task<IActionResult> Calculate(
    [FromBody] CreateQuoteDto dto)
        {
            var result =
                await _quoteService.CalculateAsync(dto);

            return Ok(result);
        }
        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Customer")]
        public async Task<IActionResult> Create(
            [FromBody] CreateQuoteDto dto)
        {
            var quote = await _quoteService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = quote.Id },
                quote);
        }

        
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateQuoteDto dto)
        {
            await _quoteService.UpdateAsync(id, dto);

            return NoContent();
        }

        
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _quoteService.DeleteAsync(id);

            return NoContent();
        }
        
        [HttpPost("{id:guid}/offer")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Offer(
            Guid id,
            [FromServices] INotificationService notificationService)
        {
            var quote = await _quoteService.GetByIdAsync(id);

            if (quote == null)
            {
                return NotFound();
            }

            if (quote.Status != QuoteStatus.Draft)
            {
                return BadRequest(new
                {
                    message = "Sadece taslak teklifler müşteriye sunulabilir."
                });
            }

            await _quoteService.ChangeStatusAsync(id, QuoteStatus.Offered);

            await notificationService.CreateAsync(
                new Kasko.Business.DTOs.Notification.NotificationCreateDto
                {
                    CustomerId = quote.CustomerId,
                    Type = "QUOTE_OFFERED",
                    Title = "Teklifiniz hazır",
                    Message = $"{quote.PremiumAmount:N2} ₺ tutarındaki kasko teklifiniz onaylandı. {quote.ValidUntil:dd.MM.yyyy} tarihine kadar satın alabilirsiniz.",
                    RelatedEntityId = quote.Id
                });

            return NoContent();
        }

        [HttpPost("{id:guid}/purchase")]
        [Authorize(Roles = "Admin,Customer")]
        public async Task<IActionResult> Purchase(
            Guid id,
            [FromBody] QuotePurchaseRequestDto dto,
            [FromServices] IPolicyService policyService,
            [FromServices] IPaymentService paymentService,
            [FromServices] Kasko.DataAccess.Repositories.Abstract.IUnitOfWork unitOfWork)
        {
            if (!dto.AcceptedTerms)
            {
                return BadRequest(new
                {
                    message = "Satın almak için bilgilendirme metinlerini onaylamanız gerekiyor."
                });
            }

            var quote = await _quoteService.GetByIdAsync(id);

            if (quote == null)
            {
                return NotFound();
            }

            if (quote.Status == QuoteStatus.Draft)
            {
                return BadRequest(new
                {
                    message = "Teklifiniz yetkili onayında. Onaylandığında satın alabilirsiniz."
                });
            }

            if (dto.SimulateFailure)
            {
                return BadRequest(new
                {
                    message = "Ödeme bankanız tarafından onaylanmadı. Kart bilgilerinizi kontrol edip tekrar deneyin veya başka bir kart kullanın."
                });
            }

            if (quote.Status != QuoteStatus.Offered)
            {
                return BadRequest(new
                {
                    message = "Bu teklif satın alınamaz. Süresi dolmuş veya daha önce işlem görmüş olabilir."
                });
            }


            Kasko.Business.DTOs.Policy.PolicyDto? policy = null;

            Kasko.Business.DTOs.Payment.PaymentDto? payment = null;

            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _quoteService.ChangeStatusAsync(id, QuoteStatus.Accepted);

                policy =
                    await policyService.CreateAsync(
                        new Kasko.Business.DTOs.Policy.PolicyCreateDto
                        {
                            CustomerId = quote.CustomerId,
                            VehicleId = quote.VehicleId,
                            QuoteId = id,
                            StartDate = DateTime.UtcNow,
                            EndDate = DateTime.UtcNow.AddYears(1)
                        });

                payment =
                    await paymentService.CreateAsync(
                        new Kasko.Business.DTOs.Payment.PaymentCreateDto
                        {
                            PolicyId = policy.Id,
                            SimulateFailure = false
                        });
            });

            return Ok(new
            {
                policyId = policy!.Id,
                policyNumber = policy.PolicyNumber,
                paymentId = payment!.Id
            });
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeStatus(
    Guid id,
    [FromQuery] QuoteStatus status)
        {
            await _quoteService.ChangeStatusAsync(id, status);

            return NoContent();
        }
    }
}