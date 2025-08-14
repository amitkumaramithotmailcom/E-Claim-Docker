using EClaim.Application.Enum;
using EClaim.Application.Notification.SMSService;

namespace EClaim.Application.Notification
{
    public class NotificationFactory : INotificationFactory
    {
        private readonly IServiceProvider _provider;

        public NotificationFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public INotificationService GetNotificationService(NotificationType type)
        {
            return type switch
            {
                NotificationType.EMAIL => _provider.GetRequiredService<EMAILService.EmailService>(),
                NotificationType.SMS => _provider.GetRequiredService<SmsService>(),
                _ => throw new NotImplementedException()
            };
        }
    }
}
