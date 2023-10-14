using System.IdentityModel.Tokens.Jwt;
using Medlix.Backend.API.Core.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Medlix.Backend.API.BAL
{
    public class JwtTokenClaim
    {
        public static bool AuthenticateUserForSendMessage(CommunicationModel communicationModel, AuthUser authUser, ILogger log)
        {
            log.LogInformation("authenticating user for Send Message");
            string patientId = "";
            if (authUser.IsB2CToken)
                patientId = communicationModel?.postExtension?.recipient[0]?.reference?.Split('/')?[1];
            else
                patientId = communicationModel?.postExtension?.sender.reference?.Split('/')?[1];

            log.LogInformation("patientId = " + patientId);

            if (string.IsNullOrEmpty(patientId))
                return false;

            if (authUser.IsB2CToken || authUser.PatientId == patientId || patientId == "{{patientId}}" || authUser.Role == "admin")
            {
                return true;
            }
            return false;

        }
       
        public static bool CheckHeaderAndJwtTokenForInviteUser(HttpRequest req, ILogger log)
        {
            StringValues jwt;
            jwt = !StringValues.IsNullOrEmpty(req.Query["id_token"]) ? req.Query["id_token"][0] : null;
            log.LogInformation("received JWT after b2c login" + jwt);
            var handler = new JwtSecurityTokenHandler();
            var decodedValue = handler.ReadJwtToken(jwt);
            log.LogInformation("Decoded jwt token." ,decodedValue);

            if (decodedValue != null)
            {
                var notBefore = decodedValue.Claims.Where(x => x.Type == "nbf").FirstOrDefault();
                var expires = decodedValue.Claims.Where(x => x.Type == "exp").FirstOrDefault();
                var issuer = decodedValue.Claims.Where(x => x.Type == "iss").FirstOrDefault();
                

                if (notBefore == null || expires == null || issuer == null )
                    return false;

                var res = Core.Common.UserInvitationService.ValidateToken(long.Parse(notBefore.Value), long.Parse(expires.Value));
                log.LogInformation($"The token is valid = {res} .");
                return res;
            }

            return false;
        }
    }
}
