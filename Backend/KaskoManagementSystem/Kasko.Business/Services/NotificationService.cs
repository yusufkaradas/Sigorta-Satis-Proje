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
                x =>
                    x.CustomerId == customerId &&
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

    public async Task<IEnumerable<NotificationDto>>
        GetMyNotificationsAsync()
    {
        var userIdValue =
            _httpContextAccessor.HttpContext?
                .User
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException();
        }

        var user =
            await _unitOfWork.Users
                .GetByIdAsync(userId);

        if (user?.CustomerId == null)
        {
            throw new UnauthorizedAccessException();
        }

        return await GetByCustomerIdAsync(
            user.CustomerId.Value);
    }
}