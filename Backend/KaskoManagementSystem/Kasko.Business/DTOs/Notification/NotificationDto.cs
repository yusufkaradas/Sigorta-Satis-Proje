namespace Kasko.Business.DTOs.Notification;

public class NotificationDto
{
    public Guid Id { get; set; }

    public Guid? CustomerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime? ReadDate { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public DateTime CreatedDate { get; set; }
}