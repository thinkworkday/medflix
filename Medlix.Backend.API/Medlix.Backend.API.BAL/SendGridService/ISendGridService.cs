using SendGrid;

namespace Medlix.Backend.API.BAL.SendGridService
{
    public interface ISendGridService
    {
        public Task<Response> SendInvitationEmail(string toAddress, string redeemUrl);
    }
}
