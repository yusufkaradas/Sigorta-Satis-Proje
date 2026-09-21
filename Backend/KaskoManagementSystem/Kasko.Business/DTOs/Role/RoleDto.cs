namespace Kasko.Business.DTOs.Role;

public class RoleDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int UserCount { get; set; }

    public bool IsSystem { get; set; }

}