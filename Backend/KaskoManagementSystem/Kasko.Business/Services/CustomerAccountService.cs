using System.Security.Cryptography;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.Business.Security;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class CustomerAccountService : ICustomerAccountService
{
    private readonly IUnitOfWork _unitOfWork;

    private readonly PasswordHasherService _passwordHasherService;

    public CustomerAccountService(
        IUnitOfWork unitOfWork,
        PasswordHasherService passwordHasherService)
    {
        _unitOfWork = unitOfWork;
        _passwordHasherService = passwordHasherService;
    }

    public async Task<CustomerAccountResult> CreateAccountAsync(Guid customerId)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);

        if (customer == null || customer.IsDeleted)
        {
            throw new NotFoundException("Müşteri bulunamadı.");
        }

        var linked = (await _unitOfWork.Users.FindAsync(x => x.CustomerId == customerId && !x.IsDeleted)).FirstOrDefault();

        if (linked != null)
        {
            throw new BadRequestException("Bu müşterinin zaten bir portal hesabı var.");
        }

        var role = (await _unitOfWork.Roles.FindAsync(x => x.Name == "Customer" && !x.IsDeleted)).FirstOrDefault()
            ?? throw new NotFoundException("Customer rolü bulunamadı.");

        var existing = (await _unitOfWork.Users.FindAsync(x => x.Email == customer.Email && !x.IsDeleted)).FirstOrDefault();

        if (existing != null && (existing.RoleId != role.Id || existing.CustomerId.HasValue))
        {
            throw new BadRequestException("Bu e-posta adresi başka bir kullanıcı hesabında kullanılıyor. Müşterinin e-postasını değiştirin.");
        }

        var password = GenerateTemporaryPassword();

        var user = existing ?? new User
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            CreatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        user.FirstName = customer.FirstName;
        user.LastName = customer.LastName;
        user.Email = customer.Email;
        user.PhoneNumber = customer.PhoneNumber;
        user.CustomerId = customer.Id;
        user.IsActive = customer.IsActive;
        user.PasswordHash = _passwordHasherService.HashPassword(user, password);

        if (existing == null)
        {
            await _unitOfWork.Users.AddAsync(user);
        }
        else
        {
            user.UpdatedDate = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
        }

        await _unitOfWork.SaveChangesAsync();

        return new CustomerAccountResult(user.Id, user.Email, password);
    }

    public async Task SyncUserAsync(Guid customerId)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);

        if (customer == null)
        {
            return;
        }

        var users = await _unitOfWork.Users.FindAsync(x => x.CustomerId == customerId && !x.IsDeleted);

        foreach (var user in users)
        {
            var emailTaken = await _unitOfWork.Users.AnyAsync(x => x.Email == customer.Email && x.Id != user.Id && !x.IsDeleted);

            if (emailTaken)
            {
                throw new BadRequestException("Bu e-posta adresi başka bir kullanıcı hesabında kullanılıyor.");
            }

            user.FirstName = customer.FirstName;
            user.LastName = customer.LastName;
            user.Email = customer.Email;
            user.PhoneNumber = customer.PhoneNumber;
            user.IsActive = customer.IsActive;
            user.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeactivateUserAsync(Guid customerId)
    {
        var users = await _unitOfWork.Users.FindAsync(x => x.CustomerId == customerId && !x.IsDeleted);

        foreach (var user in users)
        {
            user.IsActive = false;
            user.UpdatedDate = DateTime.UtcNow;

            await _unitOfWork.Users.UpdateAsync(user);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    private static string GenerateTemporaryPassword()
    {
        const string letters = "abcdefghjkmnpqrstuvwxyz";
        var suffix = new string(Enumerable.Range(0, 3).Select(_ => letters[RandomNumberGenerator.GetInt32(letters.Length)]).ToArray());
        return $"Kasko{RandomNumberGenerator.GetInt32(1000, 10000)}!{suffix}";
    }
}
