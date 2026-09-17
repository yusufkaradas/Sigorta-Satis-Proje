using Kasko.Business.DTOs.Notification;

namespace Kasko.Business.Interfaces;

public interface INotificationService
{
    Task CreateAsync(NotificationCreateDto dto);

    Task<IEnumerable<NotificationDto>> GetByCustomerIdAsync(Guid customerId);

    Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync();

    Task MarkAsReadAsync(Guid id);

    Task MarkAllAsReadAsync();
}