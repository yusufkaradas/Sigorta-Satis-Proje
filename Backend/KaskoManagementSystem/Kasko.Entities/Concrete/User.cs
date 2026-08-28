using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }
}