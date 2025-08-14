namespace EClaim.Application.Notification.EMAILService
{
    public interface IEmailBuilder
    {
        IEmailBuilder SetRecipient(string to);
        IEmailBuilder SetSubject(string subject);
        IEmailBuilder SetBody(string body, bool isHtml);
        EmailMessage Build();
    }
}
