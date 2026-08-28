using System.Security.Claims;
using Kasko.Business.DTOs.Customer;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Http;

namespace Kasko.Business.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CustomerService(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IEnumerable<CustomerListDto>> GetAllAsync()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();

        return customers.Select(customer => new CustomerListDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            IdentityNumber = customer.IdentityNumber,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            City = customer.City,
            District = customer.District ?? string.Empty,
            IsActive = customer.IsActive
        });
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);

        if (customer == null)
            return null;

        return new CustomerDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            IdentityNumber = customer.IdentityNumber,
            DateOfBirth = customer.DateOfBirth,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            City = customer.City,
            District = customer.District ?? string.Empty,
            IsActive = customer.IsActive
        };
    }

    public async Task CreateAsync(CreateCustomerDto dto)
    {
        var identityExists = await _unitOfWork.Customers
            .AnyAsync(x => x.IdentityNumber == dto.IdentityNumber);

        if (identityExists)
        {
            throw new BadRequestException(
                "Bu TC Kimlik No ile kayıtlı bir müşteri zaten mevcut.");
        }

        var emailExists = await _unitOfWork.Customers
            .AnyAsync(x => x.Email == dto.Email);

        if (emailExists)
        {
            throw new BadRequestException(
                "Bu email adresi ile kayıtlı bir müşteri zaten mevcut.");
        }

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdentityNumber = dto.IdentityNumber,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber ?? string.Empty,
            Address = dto.Address,
            City = dto.City,
            District = dto.District,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Customers.AddAsync(customer);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _unitOfWork.Customers
            .GetByIdAsync(dto.Id);

        if (customer == null)
        {
            throw new NotFoundException(
                "Müşteri bulunamadı.");
        }

        var identityExists = await _unitOfWork.Customers
            .AnyAsync(x =>
                x.IdentityNumber == dto.IdentityNumber &&
                x.Id != dto.Id);

        if (identityExists)
        {
            throw new BadRequestException(
                "Bu TC Kimlik No başka bir müşteri tarafından kullanılıyor.");
        }

        var emailExists = await _unitOfWork.Customers
            .AnyAsync(x =>
                x.Email == dto.Email &&
                x.Id != dto.Id);

        if (emailExists)
        {
            throw new BadRequestException(
                "Bu email adresi başka bir müşteri tarafından kullanılıyor.");
        }

        customer.FirstName = dto.FirstName;
        customer.LastName = dto.LastName;
        customer.IdentityNumber = dto.IdentityNumber;
        customer.DateOfBirth = dto.DateOfBirth;
        customer.Email = dto.Email;
        customer.PhoneNumber = dto.PhoneNumber ?? string.Empty;
        customer.Address = dto.Address;
        customer.City = dto.City;
        customer.District = dto.District;
        customer.IsActive = dto.IsActive;
        customer.UpdatedDate = DateTime.UtcNow;
        customer.CreatedDate = DateTime.UtcNow;

        await _unitOfWork.Customers.UpdateAsync(customer);

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

        var customer = await _unitOfWork.Customers
            .GetByIdAsync(id);

        if (customer == null)
        {
            throw new NotFoundException(
                "Müşteri bulunamadı.");
        }

        customer.IsDeleted = true;
        customer.DeletedDate = DateTime.UtcNow;
        customer.DeletedBy = deletedBy;

        await _unitOfWork.Customers.UpdateAsync(customer);

        await _unitOfWork.SaveChangesAsync();
    }
}