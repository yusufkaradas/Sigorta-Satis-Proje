using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
}