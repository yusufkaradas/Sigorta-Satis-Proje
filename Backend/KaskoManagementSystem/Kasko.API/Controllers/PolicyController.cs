using Kasko.Business.DTOs.Policy;
using Kasko.Business.Services.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Kasko.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PolicyController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var policies = await _policyService.GetAllAsync();

            return Ok(policies);
        }

        
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var policy = await _policyService.GetByIdAsync(id);

            if (policy == null)
            {
                return NotFound();
            }
            return Ok(policy);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] PolicyCreateDto dto)
        {
            var policy = await _policyService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = policy.Id },
                policy);
        }

        
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] PolicyUpdateDto dto)
        {
            await _policyService.UpdateAsync(id, dto);

            return NoContent();
        }

        
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            Guid? deletedBy = Guid.TryParse(
                userIdClaim?.Value,
                out var userId)
                ? userId
                : null;

            await _policyService.DeleteAsync(id, deletedBy);

            return NoContent();
        }
        [HttpGet("{id:guid}/terms-pdf")]
        public async Task<IActionResult> DownloadTermsPdf(
            Guid id,
            [FromServices] Kasko.Business.Interfaces.IPolicyPdfService pdfService)
        {
            var policy = await _policyService.GetByIdAsync(id);

            if (policy == null)
            {
                return NotFound();
            }

            var bytes = await pdfService.GenerateTermsAsync(id);

            return File(bytes, "application/pdf", $"{policy.PolicyNumber}-Genel-Sartlar.pdf");
        }

        [HttpGet("{id:guid}/pdf")]
        public async Task<IActionResult> DownloadPdf(
            Guid id,
            [FromServices] Kasko.Business.Interfaces.IPolicyPdfService pdfService)
        {
            var policy = await _policyService.GetByIdAsync(id);

            if (policy == null)
            {
                return NotFound();
            }

            if (policy.Status != Kasko.Entities.Enums.PolicyStatus.Active &&
                policy.Status != Kasko.Entities.Enums.PolicyStatus.Expired)
            {
                return BadRequest(new
                {
                    message = "Poliçe belgesi ödeme tamamlandıktan sonra oluşturulur."
                });
            }

            var pdf = await pdfService.GenerateAsync(id);

            return File(pdf, "application/pdf", $"{policy.PolicyNumber}.pdf");
        }

        [HttpGet("upcoming-renewals")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetUpcomingRenewals(
    [FromQuery] int daysAhead = 30)
        {
            var policies =
                await _policyService.GetUpcomingRenewalsAsync(daysAhead);

            return Ok(policies);
        }
        [HttpPost("{id:guid}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var userIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            Guid? cancelledBy = Guid.TryParse(
                userIdClaim?.Value,
                out var userId)
                ? userId
                : null;

            await _policyService.CancelAsync(id, cancelledBy);

            return NoContent();
        }
        [HttpPost("renew")]
        [Authorize(Roles = "Admin,Manager,Customer")]
        public async Task<IActionResult> Renew(
    [FromBody] PolicyRenewalDto dto)
        {
            var quote =
                await _policyService.RenewAsync(dto);

            return Ok(quote);
        }
    }
}