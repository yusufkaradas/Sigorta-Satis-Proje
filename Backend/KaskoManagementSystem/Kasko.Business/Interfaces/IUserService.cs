using Kasko.Business.DTOs.User;
using Kasko.DataAccess.Repositories.Abstract;

namespace Kasko.Business.Interfaces
{
    public interface IUserService 
    {
        Task<IEnumerable<UserListDto>> GetAllAsync();

        Task<UserDto?> GetByIdAsync(Guid id);

        Task CreateAsync(CreateUserDto dto);

        Task UpdateAsync(UpdateUserDto dto);

        Task DeleteAsync(Guid id);

    } 
}
