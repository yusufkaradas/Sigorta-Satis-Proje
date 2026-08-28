using Kasko.Business.DTOs.Role;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class RoleService : IRoleService
{
    private readonly IUnitOfWork _unitOfWork;

    public RoleService(IUnitOfWork _unitOfWork){
        this._unitOfWork = _unitOfWork;
    }
    public async Task<IEnumerable<RoleDto>> GetAllAsync()
    {
        var roles = await _unitOfWork.Roles.GetAllAsync();
        
        return roles.Select(role => new RoleDto
        {
            Id = role.Id,
            Name = role.Name
        });
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);  

        if (role == null)
        
            return null;
     
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name
        };
    }

    public async Task CreateAsync(CreateRoleDto dto)
    {
        bool Exists = await _unitOfWork.Roles.AnyAsync(x => x.Name == dto.Name);
        if (Exists)
        {
            throw new Exception("Bu rol zaten mevcut.");

        }

        var role = new Role
        {
            Name = dto.Name,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false,
        };

        await _unitOfWork.Roles.AddAsync(role);
        await _unitOfWork.SaveChangesAsync();
    }
    public async Task UpdateAsync(UpdateRoleDto dto)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(dto.Id);

        if (role == null)
            throw new Exception("Rol bulunamadı.");

        role.Name = dto.Name;
        role.UpdatedDate = DateTime.UtcNow;

        await _unitOfWork.Roles.UpdateAsync(role);

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task DeleteAsync(Guid id)
    {
        var role = await _unitOfWork.Roles.GetByIdAsync(id);

        if (role == null)
            throw new Exception("Rol bulunamadı.");

        await _unitOfWork.Roles.DeleteAsync(role);

        await _unitOfWork.SaveChangesAsync();
    }
}