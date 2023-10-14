using SendGrid;
using SendGrid.Helpers.Mail;

namespace Medlix.Backend.API.BAL.SendGridService
{
    public class SendGridService : ISendGridService
    {
        public async Task<Response> SendInvitationEmail(string toAddress, string redeemUrl)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("SendGridKey");
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress("no-reply-dev@medlix.org");
                var subject = "Invitation.";
                var to = new EmailAddress(toAddress);
                var plainTextContent = "";
                var htmlContent = $"<p>Hi</p><p>Click the following link to proceed.</p><strong><p><a href=\"{redeemUrl}\">{redeemUrl}</a></p></strong>";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);
                return response;
            }
            catch (Exception ex)
            {

                throw;
            }
        }

    }
}
