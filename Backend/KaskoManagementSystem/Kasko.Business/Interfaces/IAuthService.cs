using Kasko.Business.DTOs.Auth;

namespace Kasko.Business.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto> LoginAsync(LoginDto dto);
    }
}
