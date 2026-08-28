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
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] PolicyUpdateDto dto)
        {
            await _policyService.UpdateAsync(id, dto);

            return NoContent();
        }

        
        [HttpDelete("{id:guid}")]
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
        [HttpPost("{id:guid}/cancel")]
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
        [HttpPost("{id:guid}/expire")]
        public async Task<IActionResult> Expire(Guid id)
        {
            await _policyService.ExpireAsync(id);

            return NoContent();
        }
    }
}