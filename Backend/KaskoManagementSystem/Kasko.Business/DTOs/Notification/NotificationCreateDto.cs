namespace Kasko.Business.DTOs.Notification;

public class NotificationCreateDto
{
    public Guid CustomerId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid? RelatedEntityId { get; set; }
}