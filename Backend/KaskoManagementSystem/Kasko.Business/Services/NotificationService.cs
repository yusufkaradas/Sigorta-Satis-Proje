using System.Linq.Expressions;
using System.Security.Claims;
using Kasko.Business.DTOs.Notification;
using Kasko.Business.Exceptions;
using Kasko.Business.Interfaces;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Microsoft.AspNetCore.Http;

namespace Kasko.Business.Services;

public class NotificationService : INotificationService
{
    private const int MaxListCount = 50;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationService(
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task CreateAsync(
        NotificationCreateDto dto)
    {
        await _unitOfWork.Notifications.AddAsync(new Notification
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
        });

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<IEnumerable<NotificationDto>>
        GetByCustomerIdAsync(Guid customerId)
    {
        return Map(await _unitOfWork.Notifications.FindAsync(
            x => x.CustomerId == customerId && !x.IsDeleted));
    }

    public async Task<IEnumerable<NotificationDto>>
        GetMyNotificationsAsync()
    {
        var filter = await GetRecipientFilterAsync();

        return Map(await _unitOfWork.Notifications.FindAsync(filter));
    }

    public async Task MarkAsReadAsync(Guid id)
    {
        var filter = (await GetRecipientFilterAsync()).Compile();

        var notification = await _unitOfWork.Notifications.GetByIdAsync(id);

        if (notification == null || !filter(notification))
        {
            throw new NotFoundException("Bildirim bulunamadı.");
        }

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        notification.ReadDate = DateTime.UtcNow;

        await _unitOfWork.Notifications.UpdateAsync(notification);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync()
    {
        var filter = await GetRecipientFilterAsync();

        var unread = (await _unitOfWork.Notifications.FindAsync(filter))
            .Where(x => !x.IsRead)
            .ToList();

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadDate = DateTime.UtcNow;

            await _unitOfWork.Notifications.UpdateAsync(notification);
        }

        if (unread.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }
    }

    private static IEnumerable<NotificationDto> Map(IEnumerable<Notification> notifications)
    {
        return notifications
            .OrderByDescending(x => x.CreatedDate)
            .Take(MaxListCount)
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
            })
            .ToList();
    }

    private async Task<Expression<Func<Notification, bool>>> GetRecipientFilterAsync()
    {
        var userIdValue = _httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException();
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);

        if (user == null || user.IsDeleted)
        {
            throw new UnauthorizedAccessException();
        }

        if (user.CustomerId.HasValue)
        {
            var customerId = user.CustomerId.Value;

            return x => !x.IsDeleted && x.CustomerId == customerId;
        }

        return x => !x.IsDeleted && x.UserId == userId;
    }
}
