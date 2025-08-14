
namespace EClaim.Application.Notification.SMSService
{
    public interface ISmsBuilder
    {
        ISmsBuilder SetRecipient(string to);
        ISmsBuilder SetSubject(string subject);
        ISmsBuilder SetBody(string body, bool isHtml);
        SMSMessage Build();
    }
}
