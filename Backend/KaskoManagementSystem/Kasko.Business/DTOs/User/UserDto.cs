namespace Kasko.Business.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = String.Empty;

    public string LastName { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public string? PhoneNumber { get; set; }

    public Guid RoleId { get; set; }

    public string? RoleName { get; set; }

    public bool IsActive { get; set; }

}