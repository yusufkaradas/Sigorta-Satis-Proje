using System.Security.Claims;
using Kasko.Business.DTOs.Customer;
using Kasko.Business.DTOs.QuickQuote;
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

    private async Task<Guid?> GetCurrentCustomerIdAsync()
    {
        var userIdValue =
            _httpContextAccessor.HttpContext?.User
                .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?
                .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        return user?.CustomerId;
    }

    public async Task<IEnumerable<CustomerListDto>> GetAllAsync()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();

        var accountCustomerIds = (await _unitOfWork.Users.FindAsync(x => x.CustomerId != null && !x.IsDeleted))
            .Select(x => x.CustomerId!.Value)
            .ToHashSet();

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
            IsActive = customer.IsActive,
            HasAccount = accountCustomerIds.Contains(customer.Id)
        });
    }

    public async Task<CustomerDto?> GetCurrentAsync()
    {
        var customerId = await GetCurrentCustomerIdAsync();

        return customerId == null
            ? null
            : await GetByIdAsync(customerId.Value);
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id)
    {
        if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true &&
            await GetCurrentCustomerIdAsync() != id)
        {
            return null;
        }

        var customer = await _unitOfWork.Customers.GetByIdAsync(id);

        if (customer == null)
            return null;

        var hasAccount = _unitOfWork.Users != null &&
            await _unitOfWork.Users.AnyAsync(x => x.CustomerId == customer.Id && !x.IsDeleted);

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
            HasAccount = hasAccount,
            IsActive = customer.IsActive
        };
    }

    public async Task<Guid> CreateAsync(CreateCustomerDto dto)
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

        return customer.Id;
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

        if (_httpContextAccessor.HttpContext?.User.IsInRole("Customer") == true)
        {
            if (await GetCurrentCustomerIdAsync() != customer.Id)
            {
                throw new NotFoundException(
                    "Müşteri bulunamadı.");
            }

            dto.FirstName = customer.FirstName;
            dto.LastName = customer.LastName;
            dto.IdentityNumber = customer.IdentityNumber;
            dto.DateOfBirth = customer.DateOfBirth;
            dto.Email = customer.Email;
            dto.IsActive = customer.IsActive;
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

        await _unitOfWork.Customers.UpdateAsync(customer);

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task<QuickQuoteCustomerLookupResponseDto>
    GetForQuickQuoteAsync(
        string identityNumber,
        string phoneNumber)
    {
        var normalizedIdentityNumber =
            new string(
                identityNumber
                    .Where(char.IsDigit)
                    .ToArray());

        var normalizedPhoneNumber =
            new string(
                phoneNumber
                    .Where(char.IsDigit)
                    .ToArray());

        var customers =
            await _unitOfWork.Customers.FindAsync(
                x =>
                    x.IdentityNumber ==
                    normalizedIdentityNumber);

        var customer =
            customers.FirstOrDefault();

        if (customer == null)
        {
            return new QuickQuoteCustomerLookupResponseDto
            {
                Found = false
            };
        }

        var storedPhone =
            new string(
                (customer.PhoneNumber ?? string.Empty)
                    .Where(char.IsDigit)
                    .ToArray());

        var inputPhone =
            normalizedPhoneNumber;

        if (
            storedPhone.StartsWith("90") &&
            storedPhone.Length == 12)
        {
            storedPhone =
                "0" +
                storedPhone[2..];
        }

        if (
            inputPhone.StartsWith("90") &&
            inputPhone.Length == 12)
        {
            inputPhone =
                "0" +
                inputPhone[2..];
        }

        var storedLast10 = storedPhone.Length >= 10 ? storedPhone[^10..] : storedPhone;
        var inputLast10 = inputPhone.Length >= 10 ? inputPhone[^10..] : inputPhone;

        if (storedLast10 != inputLast10 && !customer.IsDeleted)
        {
            throw new BadRequestException("Bu T.C. Kimlik No sistemimizde farklı bir telefon numarasıyla kayıtlı. Lütfen kayıtlı cep telefonunuzu girin veya hesabınızla giriş yapın.");
        }

        if (storedLast10 != inputLast10)
        {
            return new QuickQuoteCustomerLookupResponseDto
            {
                Found = false
            };
        }

        if (!customer.IsActive || customer.IsDeleted)
        {
            return new QuickQuoteCustomerLookupResponseDto
            {
                Found = false
            };
        }

        return new QuickQuoteCustomerLookupResponseDto
        {
            Found = true,

            CustomerId = customer.Id,

            FirstName = customer.FirstName,

            LastName = customer.LastName,

            Email = customer.Email ?? string.Empty
        };
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
    public async Task<Guid> CreateForQuickQuoteAsync(
    QuickQuoteCustomerCreateRequestDto dto)
    {
        var identityNumber =
            new string(
                dto.IdentityNumber
                    .Where(char.IsDigit)
                    .ToArray());

        var phoneNumber =
            new string(
                dto.PhoneNumber
                    .Where(char.IsDigit)
                    .ToArray());

        var identityExists =
            await _unitOfWork.Customers.AnyAsync(
                x => x.IdentityNumber == identityNumber);

        if (identityExists)
        {
            throw new BadRequestException(
                "Bu T.C. Kimlik No ile kayıtlı bir müşteri zaten mevcut.");
        }

        var customer = new Customer
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IdentityNumber = identityNumber,
            DateOfBirth = dto.DateOfBirth,
            Email = dto.Email,
            PhoneNumber =
                phoneNumber.StartsWith("90") &&
                phoneNumber.Length == 12
                    ? $"+{phoneNumber}"
                    : phoneNumber.StartsWith("0")
                        ? phoneNumber
                        : $"0{phoneNumber}",
            Address = dto.Address,
            City = dto.City,
            District = dto.District,
            IsActive = true,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Customers.AddAsync(customer);

        await _unitOfWork.SaveChangesAsync();

        return customer.Id;
    }
}