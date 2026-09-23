using Kasko.Entities.Abstract;

namespace Kasko.Entities.Concrete;

public class Notification : BaseEntity
{
    public Guid? CustomerId { get; set; }

    public Guid? UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime? ReadDate { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public Customer? Customer { get; set; }

    public User? User { get; set; }
}