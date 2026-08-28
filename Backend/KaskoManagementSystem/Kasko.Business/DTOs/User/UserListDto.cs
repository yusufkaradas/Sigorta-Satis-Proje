namespace Kasko.Business.DTOs.User;

public class UserListDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = String.Empty;
    public string LastName { get; set; } = String.Empty;
    public string Email { get; set; } = String.Empty;
    public bool IsActive { get; set; }
}
