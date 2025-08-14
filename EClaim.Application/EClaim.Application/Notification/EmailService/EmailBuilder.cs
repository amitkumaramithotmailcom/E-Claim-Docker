namespace EClaim.Application.Notification.EMAILService
{
    public class EmailBuilder : IEmailBuilder
    {
        private EmailMessage _email = new();

        public IEmailBuilder SetRecipient(string to)
        {
            _email.To = to;
            return this;
        }

        public IEmailBuilder SetSubject(string subject)
        {
            _email.Subject = subject;
            return this;
        }

        public IEmailBuilder SetBody(string body, bool isHtml)
        {
            _email.Body = body;
            _email.IsHtml = isHtml;
            return this;
        }

        public EmailMessage Build()
        {
            return _email;
        }
    }
}
