using EClaim.Application.Enum;

namespace EClaim.Application.Notification
{
    public interface INotificationFactory
    {
        INotificationService GetNotificationService(NotificationType type);
    }
}
