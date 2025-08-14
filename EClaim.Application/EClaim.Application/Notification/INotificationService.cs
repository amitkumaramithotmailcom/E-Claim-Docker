namespace EClaim.Application.Notification;

public interface INotificationService
{
    Task SendNotificationlAsync<T>(T content);
}
