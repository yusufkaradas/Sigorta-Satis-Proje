using Kasko.Business.DTOs.Role;
namespace Kasko.Business.Interfaces
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllAsync();

        Task<RoleDto?> GetByIdAsync(Guid id);

        Task CreateAsync(CreateRoleDto roleDto);

        Task UpdateAsync(UpdateRoleDto roleDto);

        Task DeleteAsync(Guid id);
    }
}
