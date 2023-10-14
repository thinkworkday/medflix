using System.Web;
using Microsoft.IdentityModel.Tokens;
namespace Medlix.Backend.API.Core.Common
{
    public static class UserInvitationService
    {
        public static string GenerateSignedRedeemUrl(string emailAddress, TimeSpan InvitationTokenLifetime,string baseUrl, string patientId)
        {
            var now = DateTime.UtcNow;
            var notBefore = EpochTime.GetIntDate(now);
            var expires = EpochTime.GetIntDate(now.Add(InvitationTokenLifetime));
            var code = $"{emailAddress}|{notBefore}|{expires}|{patientId}|X";
            var signature = Unicode.ComputeSha256Hash(code);
            return GenerateRedeemUrl(emailAddress, notBefore, expires, signature, baseUrl, patientId);
        }

        public static bool ValidateSignedRedeemUrl(string emailAddress, long notBefore, long expires, string theSignature, string patientId)
        {
            var code = $"{emailAddress}|{notBefore}|{expires}|{patientId}|X";
            var computeHashCode = Unicode.ComputeSha256Hash(code);
            var now = EpochTime.GetIntDate(DateTime.UtcNow);
            return string.Equals(computeHashCode, theSignature) && now >= notBefore && now < expires;
        }
        public static bool ValidateToken(long notBefore,long expires)
        {
            var now = EpochTime.GetIntDate(DateTime.UtcNow);
            return (now >= notBefore && now < expires);
        }
        public static string GenerateRedeemUrl(string emailAddress, long notBefore, long expires, string signature, string baseUrl, string patientId)
        {

            var builder = new UriBuilder(@$"{baseUrl}/Redeem");
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["email"] = emailAddress;
            query["nbf"] = notBefore.ToString();
            query["exp"] = expires.ToString();
            query["sig"] = signature;
            query["baseUrl"] = baseUrl;
            query["patientId"] = patientId;
            builder.Query = query.ToString();
            return builder.ToString();
        }
        public static string GenerateSignUpUrl(string hostName, string tenantName, string clientId, string nonce, string  redirectUri, string emailAddress, string displayName)
        {
            try
            {
                string url = string.Format("https://{0}/{1}/oauth2/v2.0/authorize?p=B2C_1_Signup&client_id={2}&nonce={3}" +
                 "&redirect_uri={4}&response_mode=query&scope=openid&response_type=id_token&disable_cache=true&login_hint={5}&displayName={6}&prompt=login"
                 , hostName, tenantName, clientId, nonce, redirectUri, emailAddress, displayName);
                return url;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
