using Kasko.Business.DTOs.Quote;
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

        
        [HttpPost]
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
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateQuoteDto dto)
        {
            await _quoteService.UpdateAsync(id, dto);

            return NoContent();
        }

        
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _quoteService.DeleteAsync(id);

            return NoContent();
        }
        
        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> ChangeStatus(
    Guid id,
    [FromQuery] QuoteStatus status)
        {
            await _quoteService.ChangeStatusAsync(id, status);

            return NoContent();
        }
    }
}