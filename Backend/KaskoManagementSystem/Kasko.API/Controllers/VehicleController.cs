using Kasko.Business.DTOs.Vehicle;
using Kasko.Business.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var vehicles = await _vehicleService.GetAllAsync();

            return Ok(vehicles);
        }

        
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id);

            if (vehicle == null)
            {
                return NotFound(new
                {
                    StatusCodes = 404,
                    Message = "Araç bulunamadı."
                });
            }

            return Ok(vehicle);
        }

        
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] CreateVehicleDto dto)
        {
            var vehicle = await _vehicleService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = vehicle.Id },
                vehicle); ;
        }

        
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] UpdateVehicleDto dto)
        {
            await _vehicleService.UpdateAsync(id, dto);

            return NoContent();
        }

        
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _vehicleService.DeleteAsync(id);

            return NoContent();
            
        }
    }
}