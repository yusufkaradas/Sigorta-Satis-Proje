using Kasko.Business.DTOs.Notification;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Services;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public NotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateAsync(
        NotificationCreateDto dto)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            CustomerId = dto.CustomerId,
            Type = dto.Type,
            Title = dto.Title,
            Message = dto.Message,
            RelatedEntityId = dto.RelatedEntityId,
            IsRead = false,
            ReadDate = null,
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.Notifications
            .AddAsync(notification);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<NotificationDto>>
        GetByCustomerIdAsync(Guid customerId)
    {
        var notifications =
            await _unitOfWork.Notifications.FindAsync(
                x => x.CustomerId == customerId &&
                     !x.IsDeleted);

        return notifications
            .OrderByDescending(x => x.CreatedDate)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                Type = x.Type,
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                ReadDate = x.ReadDate,
                RelatedEntityId = x.RelatedEntityId,
                CreatedDate = x.CreatedDate
            });
    }
}