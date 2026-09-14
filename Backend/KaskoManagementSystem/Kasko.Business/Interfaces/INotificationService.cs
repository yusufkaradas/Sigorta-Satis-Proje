using Kasko.Business.DTOs.Notification;

namespace Kasko.Business.Interfaces;

public interface INotificationService
{
    Task CreateAsync(NotificationCreateDto dto);

    Task<IEnumerable<NotificationDto>> GetByCustomerIdAsync(Guid customerId);
}