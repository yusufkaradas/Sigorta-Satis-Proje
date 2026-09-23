using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.Business.Notifications;

public static class NotificationWriter
{
    public static readonly string[] AdminOnly = { "Admin" };

    public static readonly string[] Staff = { "Admin", "Manager" };

    private static readonly TimeZoneInfo TurkeyTimeZone = ResolveTurkeyTimeZone();

    public static string FormatDate(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, TurkeyTimeZone).ToString("dd.MM.yyyy");
    }

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    public static Task ToCustomerAsync(
        IUnitOfWork unitOfWork,
        Guid customerId,
        string type,
        string title,
        string message,
        Guid? relatedEntityId = null)
    {
        return unitOfWork.Notifications.AddAsync(
            Build(customerId, null, type, title, message, relatedEntityId));
    }

    public static Task ToUserAsync(
        IUnitOfWork unitOfWork,
        Guid userId,
        string type,
        string title,
        string message,
        Guid? relatedEntityId = null)
    {
        return unitOfWork.Notifications.AddAsync(
            Build(null, userId, type, title, message, relatedEntityId));
    }

    public static async Task ToRolesAsync(
        IUnitOfWork unitOfWork,
        IReadOnlyCollection<string> roleNames,
        string type,
        string title,
        string message,
        Guid? relatedEntityId = null,
        Guid? exceptUserId = null)
    {
        var roleIds =
            (await unitOfWork.Roles.FindAsync(x => roleNames.Contains(x.Name) && !x.IsDeleted))
            .Select(x => x.Id)
            .ToList();

        if (roleIds.Count == 0)
        {
            return;
        }

        var users =
            await unitOfWork.Users.FindAsync(x =>
                roleIds.Contains(x.RoleId) &&
                x.IsActive &&
                !x.IsDeleted);

        foreach (var user in users.Where(x => x.Id != exceptUserId))
        {
            await unitOfWork.Notifications.AddAsync(
                Build(null, user.Id, type, title, message, relatedEntityId));
        }
    }

    private static Notification Build(
        Guid? customerId,
        Guid? userId,
        string type,
        string title,
        string message,
        Guid? relatedEntityId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            IsDeleted = false,
            CreatedDate = DateTime.UtcNow
        };
    }
}
