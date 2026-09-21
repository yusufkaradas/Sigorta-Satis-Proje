using Kasko.Business.DTOs.Role;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class RoleService : IRoleService
{
    public static readonly string[] SystemRoles = { "Admin", "Manager", "Customer" };

    private readonly IUnitOfWork _unitOfWork;

    public RoleService(IUnitOfWork _unitOfWork){
        this._unitOfWork = _unitOfWork;
    }

    private static bool IsSystemRole(string name) =>
        SystemRoles.Contains(name, StringComparer.OrdinalIgnoreCase);

    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _unitOfWork.Roles.GetAllAsync();
        var users = (await _unitOfWork.Users.FindAsync(x => !x.IsDeleted)).ToList();

        return roles
            .Where(role => !role.IsDeleted)
            .Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                UserCount = users.Count(x => x.RoleId == role.Id),
                IsSystem = IsSystemRole(role.Name)
            });
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);

        if (role == null || role.IsDeleted)
            return null;

        var userCount = (await _unitOfWork.Users.FindAsync(x => x.RoleId == id && !x.IsDeleted)).Count();

        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            UserCount = userCount,
            IsSystem = IsSystemRole(role.Name)
        };
    }

    public async Task CreateAsync(CreateRoleDto dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;

        if (name.Length < 2)
        {
            throw new BadRequestException("Rol adı en az 2 karakter olmalıdır.");
        }

        bool exists = await _unitOfWork.Roles.AnyAsync(x => x.Name == name && !x.IsDeleted);
        if (exists)
        {
            throw new BadRequestException("Bu rol zaten mevcut.");
        }

        var role = new Role
        {
            Name = name,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false,
        };

        await _unitOfWork.Roles.AddAsync(role);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(UpdateRoleDto dto)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(dto.Id);

        if (role == null || role.IsDeleted)
            throw new NotFoundException("Rol bulunamadı.");

        if (IsSystemRole(role.Name))
            throw new BadRequestException("Sistem rolleri (Admin, Manager, Customer) yeniden adlandırılamaz.");

        var name = dto.Name?.Trim() ?? string.Empty;

        if (name.Length < 2)
            throw new BadRequestException("Rol adı en az 2 karakter olmalıdır.");

        if (await _unitOfWork.Roles.AnyAsync(x => x.Name == name && x.Id != dto.Id && !x.IsDeleted))
            throw new BadRequestException("Bu rol zaten mevcut.");

        role.Name = name;
        role.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Roles.UpdateAsync(role);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);

        if (role == null || role.IsDeleted)
            throw new NotFoundException("Rol bulunamadı.");

        if (IsSystemRole(role.Name))
            throw new BadRequestException("Sistem rolleri (Admin, Manager, Customer) silinemez.");

        if (await _unitOfWork.Users.AnyAsync(x => x.RoleId == id && !x.IsDeleted))
            throw new BadRequestException("Bu role atanmış kullanıcılar var. Önce kullanıcıların rolünü değiştirin.");

        await _unitOfWork.Roles.DeleteAsync(role);

        await _unitOfWork.SaveChangesAsync();
    }
}
