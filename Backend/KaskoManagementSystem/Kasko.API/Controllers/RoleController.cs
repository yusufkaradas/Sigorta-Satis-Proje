using Kasko.Business.DTOs.Role;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kasko.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]

    public class RoleController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleController(IRoleService roleService)
        {
            _roleService = roleService;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleService.GetAllAsync();
            return Ok(roles);
        }
        
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var role = await _roleService.GetByIdAsync(id);
            if (role == null)
                return NotFound();
            return Ok(role);
        }
        
        [HttpPost]
       
        public async Task<IActionResult> Create(CreateRoleDto dto)
        {
            await _roleService.CreateAsync(dto);
            return StatusCode(StatusCodes.Status201Created); 
        }
        
        [HttpPut]
        
        public async Task<IActionResult> Update(UpdateRoleDto dto)
        {
            await _roleService.UpdateAsync(dto);
            return NoContent();
        }
       
        [HttpDelete("{id:guid}")]
        
        public async Task<IActionResult> Delete(Guid id)
        {
            await _roleService.DeleteAsync(id);
            return NoContent();
        }
    }
}
