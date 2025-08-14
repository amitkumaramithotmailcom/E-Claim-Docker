using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace EClaim.Application.Notification.SMSService
{
    public class SmsService : INotificationService
    {
        private readonly IConfiguration _config;

        public SmsService(IConfiguration config)
        {
            _config = config;

            TwilioClient.Init(
                _config["Twilio:AccountSid"],
                _config["Twilio:AuthToken"]
            );
        }
        public Task SendNotificationlAsync<T>(T content)
        {
            if (content is SMSMessage sms)
            {
                var from = new PhoneNumber(_config["Twilio:FromPhone"]);
                var toNumber = new PhoneNumber(sms.Phone);

                var msg = MessageResource.CreateAsync(
                    to: toNumber,
                    from: from,
                    body: sms.Body
                );
            }
            else
            {
                throw new InvalidOperationException("Unsupported notification type");
            }
            return Task.CompletedTask;
        }
    }
}
