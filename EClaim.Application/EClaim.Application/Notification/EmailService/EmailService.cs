using System.Net;
using System.Net.Mail;

namespace EClaim.Application.Notification.EMAILService;

public class EmailService : INotificationService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public Task SendNotificationlAsync<T>(T content)
    {
        if (content is EmailMessage email)
        {
            var smtpClient = new SmtpClient(_config["Smtp:Host"])
            {
                Port = int.Parse(_config["Smtp:Port"]),
                Credentials = new NetworkCredential(_config["Smtp:User"], _config["Smtp:Pass"]),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_config["Smtp:From"]),
                Subject = email.Subject,
                Body = email.Body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(email.To);

            smtpClient.SendMailAsync(mailMessage);
        }
        else
        {
            throw new InvalidOperationException("Unsupported notification type");
        }
        return Task.CompletedTask;
    }
}
