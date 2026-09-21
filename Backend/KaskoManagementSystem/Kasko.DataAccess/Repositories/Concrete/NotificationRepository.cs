using Kasko.DataAccess.Repositories.Abstract;
using Kasko.Entities.Concrete;

namespace Kasko.DataAccess.Repositories.Concrete;

public class NotificationRepository
    : GenericRepository<Notification>,
      INotificationRepository
{
    public NotificationRepository(KaskoContext context)
        : base(context)
    {
    }
}