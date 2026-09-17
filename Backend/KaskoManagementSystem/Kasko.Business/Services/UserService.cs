using Kasko.Business.DTOs.User;
using Kasko.Business.Interfaces;
using Kasko.Business.Security;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Kasko.Business.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Kasko.Business.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasherService _passwordHasherService;

    public UserService(
        IUnitOfWork unitOfWork,
        PasswordHasherService passwordHasherService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _passwordHasherService = passwordHasherService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<UserListDto>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();

        return users.Select(user => new UserListDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            IsActive = user.IsActive
        });
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _unitOfWork.Users.GetByIdWithRoleAsync(id);

        if (user == null)
            return null;

        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name,
            IsActive = user.IsActive
        };
    }

    public async Task CreateAsync(CreateUserDto dto)
    {
        var emailExists = await _unitOfWork.Users
            .AnyAsync(x => x.Email == dto.Email);

        if (emailExists)
        {
            throw new BadRequestException("Bu email adresi zaten kayıtlı.");
        }

        var role = await _unitOfWork.Roles
            .GetByIdAsync(dto.RoleId);

        if (role == null)
        {
            throw new NotFoundException("Rol bulunamadı.");
        }
        Customer? customer = null;

        if (role.Name == "Customer")
        {
            if (string.IsNullOrWhiteSpace(dto.IdentityNumber) || !dto.DateOfBirth.HasValue)
            {
                throw new BadRequestException("Müşteri rolündeki kullanıcı için T.C. Kimlik No, doğum tarihi ve adres bilgileri zorunludur.");
            }

            var identityExists = await _unitOfWork.Customers
                .AnyAsync(x => x.IdentityNumber == dto.IdentityNumber && !x.IsDeleted);

            if (identityExists)
            {
                throw new BadRequestException("Bu T.C. Kimlik No ile kayıtlı bir müşteri zaten var.");
            }

            var customerEmailExists = await _unitOfWork.Customers
                .AnyAsync(x => x.Email == dto.Email && !x.IsDeleted);

            if (customerEmailExists)
            {
                throw new BadRequestException("Bu e-posta adresi başka bir müşteri kaydında kullanılıyor.");
            }

            customer = new Customer
            {
                Id = Guid.NewGuid(),
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                IdentityNumber = dto.IdentityNumber.Trim(),
                DateOfBirth = dto.DateOfBirth.Value.Date,
                Email = dto.Email.Trim(),
                PhoneNumber = dto.PhoneNumber ?? string.Empty,
                City = dto.City?.Trim() ?? string.Empty,
                District = dto.District?.Trim() ?? string.Empty,
                Address = dto.Address?.Trim() ?? string.Empty,
                IsActive = true,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };

            await _unitOfWork.Customers.AddAsync(customer);
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            RoleId = dto.RoleId,
            CustomerId = customer?.Id,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHasherService.HashPassword(user, dto.Password);

        await _unitOfWork.Users.AddAsync(user);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(UpdateUserDto dto)
    {

        var user = await _unitOfWork.Users.GetByIdAsync(dto.Id);

        if (user == null)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        
        var emailExists = await _unitOfWork.Users
            .AnyAsync(x => x.Email == dto.Email && x.Id != dto.Id);

        if (emailExists)
        {
            throw new BadRequestException(
            "Bu email adresi başka bir kullanıcı tarafından kullanılıyor.");
        }

       
        var role = await _unitOfWork.Roles
            .GetByIdAsync(dto.RoleId);

        if (role == null)
        {
            throw new NotFoundException("Rol bulunamadı.");
        }

       
        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Email = dto.Email;
        user.PhoneNumber = dto.PhoneNumber;
        user.RoleId = dto.RoleId;
        user.IsActive = dto.IsActive;
        user.UpdatedDate = DateTime.UtcNow;

        if (user.CustomerId.HasValue)
        {
            var linkedCustomer = await _unitOfWork.Customers.GetByIdAsync(user.CustomerId.Value);

            if (linkedCustomer != null)
            {
                var customerEmailTaken = await _unitOfWork.Customers
                    .AnyAsync(x => x.Email == dto.Email && x.Id != linkedCustomer.Id && !x.IsDeleted);

                if (customerEmailTaken)
                {
                    throw new BadRequestException("Bu e-posta adresi başka bir müşteri kaydında kullanılıyor.");
                }

                linkedCustomer.FirstName = dto.FirstName;
                linkedCustomer.LastName = dto.LastName;
                linkedCustomer.Email = dto.Email;
                linkedCustomer.PhoneNumber = dto.PhoneNumber ?? linkedCustomer.PhoneNumber;
                linkedCustomer.IsActive = dto.IsActive;
                linkedCustomer.UpdatedDate = DateTime.UtcNow;

                await _unitOfWork.Customers.UpdateAsync(linkedCustomer);
            }
        }

        
        await _unitOfWork.Users.UpdateAsync(user);

        
        await _unitOfWork.SaveChangesAsync();
    }
    public async Task DeleteAsync(Guid id)
    {
        var deletedByValue = _httpContextAccessor.HttpContext?
            .User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        Guid? deletedBy = Guid.TryParse(
            deletedByValue,
            out var deletedById)
            ? deletedById
            : null;

        var existingUser = await _unitOfWork.Users.GetByIdAsync(id);

        var result = await _unitOfWork.Users
            .DeleteUserAsync(id, deletedBy);

        if (!result)
        {
            throw new NotFoundException("Kullanıcı bulunamadı.");
        }

        if (existingUser?.CustomerId != null)
        {
            var linkedCustomer = await _unitOfWork.Customers.GetByIdAsync(existingUser.CustomerId.Value);

            if (linkedCustomer != null)
            {
                linkedCustomer.IsActive = false;
                linkedCustomer.UpdatedDate = DateTime.UtcNow;

                await _unitOfWork.Customers.UpdateAsync(linkedCustomer);
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
    
    private readonly IHttpContextAccessor _httpContextAccessor;

}