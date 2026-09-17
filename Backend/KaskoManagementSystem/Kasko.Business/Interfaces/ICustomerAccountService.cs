namespace Kasko.Business.Interfaces;

public record CustomerAccountResult(Guid UserId, string Email, string TemporaryPassword);

public interface ICustomerAccountService
{
    Task<CustomerAccountResult> CreateAccountAsync(Guid customerId);

    Task SyncUserAsync(Guid customerId);

    Task DeactivateUserAsync(Guid customerId);
}
