using Kasko.Business.DTOs.User;
using Kasko.Business.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace Kasko.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]

    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        public UserController(IUserService userService)
        {
            _userService = userService;
        }
    
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }
        
        
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            return Ok(user);

        }
        
        [HttpPost]
        

        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            await _userService.CreateAsync(dto);
            
            return StatusCode(StatusCodes.Status201Created);
        }
        
        [HttpPut]
       

        public async Task<IActionResult> Update([FromBody]UpdateUserDto dto)
        {
            await _userService.UpdateAsync(dto);
            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        
        public async Task<IActionResult> Delete(Guid id)
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }

    }
}