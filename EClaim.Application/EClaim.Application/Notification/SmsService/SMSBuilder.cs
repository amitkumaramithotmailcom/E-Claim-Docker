namespace EClaim.Application.Notification.SMSService
{
    public class SMSBuilder : ISmsBuilder
    {
        private SMSMessage _sms = new();
        public ISmsBuilder SetRecipient(string phone)
        {
            _sms.Phone = phone;
            return this;
        }

        public ISmsBuilder SetSubject(string subject)
        {
            _sms.Subject = subject;
            return this;
        }

        public ISmsBuilder SetBody(string body, bool isHtml)
        {
            _sms.Body = body;
            return this;
        }

        public SMSMessage Build()
        {
            return _sms;
        }
    }
}
