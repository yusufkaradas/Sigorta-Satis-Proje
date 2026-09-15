namespace Kasko.Business.DTOs.Auth;

public class RegisterDto
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string IdentityNumber { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}