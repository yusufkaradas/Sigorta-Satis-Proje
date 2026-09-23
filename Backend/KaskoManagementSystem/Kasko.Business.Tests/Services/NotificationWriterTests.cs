using System.Linq.Expressions;
using Kasko.Business.Notifications;
using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;
using Moq;

namespace Kasko.Business.Tests.Services;

public class NotificationWriterTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<INotificationRepository> _notificationRepositoryMock = new();
    private readonly List<Notification> _added = new();

    public NotificationWriterTests()
    {
        _unitOfWorkMock.Setup(x => x.Roles).Returns(_roleRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Notifications).Returns(_notificationRepositoryMock.Object);

        _notificationRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Notification>()))
            .Callback<Notification>(_added.Add)
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task ToRolesAsync_ShouldNotifyActiveUsersOfGivenRoles_ExceptRequester()
    {
        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var requesterId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _roleRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<Role, bool>>>()))
            .ReturnsAsync((Expression<Func<Role, bool>> predicate) =>
                new[] { adminRole, new Role { Id = Guid.NewGuid(), Name = "Customer" } }.Where(predicate.Compile()).ToList());

        var users = new[]
        {
            new User { Id = adminId, RoleId = adminRole.Id, IsActive = true },
            new User { Id = requesterId, RoleId = adminRole.Id, IsActive = true },
            new User { Id = Guid.NewGuid(), RoleId = adminRole.Id, IsActive = false },
            new User { Id = Guid.NewGuid(), RoleId = Guid.NewGuid(), IsActive = true }
        };

        _userRepositoryMock
            .Setup(x => x.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync((Expression<Func<User, bool>> predicate) => users.Where(predicate.Compile()).ToList());

        await NotificationWriter.ToRolesAsync(
            _unitOfWorkMock.Object,
            NotificationWriter.AdminOnly,
            "PRICING_REQUEST_CREATED",
            "Yeni fiyat değişikliği talebi",
            "Test",
            Guid.NewGuid(),
            requesterId);

        var notification = Assert.Single(_added);
        Assert.Equal(adminId, notification.UserId);
        Assert.Null(notification.CustomerId);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task ToCustomerAsync_ShouldCreateUnreadCustomerNotification()
    {
        var customerId = Guid.NewGuid();

        await NotificationWriter.ToCustomerAsync(
            _unitOfWorkMock.Object,
            customerId,
            "PAYMENT_FAILED",
            "Ödemeniz tamamlanamadı",
            "Test");

        var notification = Assert.Single(_added);
        Assert.Equal(customerId, notification.CustomerId);
        Assert.Null(notification.UserId);
        Assert.Equal("PAYMENT_FAILED", notification.Type);
    }
}
