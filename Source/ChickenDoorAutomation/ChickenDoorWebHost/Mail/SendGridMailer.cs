using System;
using System.Threading.Tasks;
using ChickenDoorDriver;
using ChickenDoorWebHost.Config;
using Driver;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace ChickenDoorWebHost.Mail
{
    public class SendGridMailer : IExternalNotification
    {
        readonly MailConfig _mailConfig;

        public SendGridMailer(MailConfig mailConfig)
        {
            _mailConfig = mailConfig;
        }

        public async Task Notify(DoorState doorState, string cameraImage)
        {
            try
            {
                var apiKey = _mailConfig.SendGridApiKey;
                var mailClient = new SendGridClient(apiKey);

                var from = new EmailAddress(_mailConfig.Sender, "Netti & Co");
                var subject = "Message from your door";
                foreach (var receiver in _mailConfig.Receivers)
                {
                    var to = new EmailAddress(receiver, "Adoorer");
                    var plainTextContent = "and easy to do anywhere, even with C#";
                    var htmlContent = @$"<strong>Door state: {doorState}</strong>

<img src=""{cameraImage}"" width=""1100"" height=""750""/>
";
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                    var response = await mailClient.SendEmailAsync(msg);
                    Log.Info($"Mail sent to {receiver} with status code {response.StatusCode}.");
                }
            }
            catch (Exception e)
            {
                Log.Error($"Failed to send mail. {e}");
            }
        }
    }
}
