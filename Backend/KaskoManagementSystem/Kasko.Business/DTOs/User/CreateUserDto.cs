namespace Kasko.Business.DTOs.User;

public class CreateUserDto {

    
    public string FirstName { get; set; } = String.Empty;
    public string LastName { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;

    public string Password { get; set; } = String.Empty;

    public string? PhoneNumber { get; set; }
    public Guid RoleId { get; set; }
    public Guid? CustomerId { get; set; }

    public string? IdentityNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Address { get; set; }



}
