using Microsoft.Graph;
using Azure.Identity;
using CareAPI.Core.DTO;

namespace Medlix.Backend.API.BAL.AzureB2CService
{
    public class AzureB2CService : IAzureB2CService
    {
        private GraphServiceClient _graphClient;
        private readonly string TenantName = Environment.GetEnvironmentVariable("TenantName");
        private readonly string ExtensionClientId = Environment.GetEnvironmentVariable("ExtensionClientId")?.Replace("-", "");
        private readonly string TenantId = Environment.GetEnvironmentVariable("TenantId");
        private readonly string ClientId = Environment.GetEnvironmentVariable("ClientId");
        private readonly string ClientSecret = Environment.GetEnvironmentVariable("ClientSecret");

        public AzureB2CService()
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            // using Azure.Identity;
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(TenantId, ClientId, ClientSecret, options);

            _graphClient = new GraphServiceClient(clientSecretCredential, scopes);

        }

        public async Task<B2CUpdatableResponse> GetCanUpdateUser(string emailAddress, string phoneNumber)
        {
            emailAddress = emailAddress.Trim();
            phoneNumber = phoneNumber.Trim();
            var users = await _graphClient.Users.Request()
                .Select($"displayName,id,mail,otherMails,mobilePhone,businessPhones,identities,extension_{ExtensionClientId.Replace("-", "")}_PatientID")
                .GetAsync();

            List<User> usersListByEmail = new List<User>();
            List<User> usersListByPhone = new List<User>();
            B2CUpdatableResponse result = new B2CUpdatableResponse();
            if (phoneNumber.StartsWith("00"))
            {
                phoneNumber = "+" + phoneNumber.Substring(2);
            }
            foreach (var user in users)
            {
                var checkUserByIdentity = user.Identities.Any(identity => identity.SignInType.Equals("emailAddress") && identity.IssuerAssignedId.Equals(emailAddress));
                var checkUserByOtherEmail = !string.IsNullOrEmpty(user.Mail) && user.Mail.Equals(emailAddress) || user.OtherMails.Any(mail => mail.Equals(emailAddress));
                if (checkUserByIdentity || checkUserByOtherEmail)
                {
                    usersListByEmail.Add(user);
                }
                var checkUserByIdentityPhone = user.Identities.Any(identity => identity.SignInType.Equals("phoneNumber") && identity.IssuerAssignedId.Contains(phoneNumber));
                var checkUserByPhone = (!string.IsNullOrEmpty(user.MobilePhone) && user.MobilePhone.Contains(phoneNumber)) || (user.BusinessPhones != null && user.BusinessPhones.Any(phone => phone.Contains(phoneNumber)));
                if (checkUserByPhone || checkUserByIdentityPhone)
                {
                    usersListByPhone.Add(user);
                }
            }
            if (usersListByEmail.Count > 1)
            {
                result.CanUpdate = false;
                result.ErrorMessage = $"There are {usersListByEmail.Count} Users with {emailAddress}";
                return result;
            }

            if (usersListByPhone.Count > 1)
            {
                result.CanUpdate = false;
                result.ErrorMessage = $"There are {usersListByPhone.Count} Users with {phoneNumber}";
                return result;
            }

            if (usersListByEmail.Count == 0)
            {
                result.CanUpdate = false;
                result.ErrorMessage = $"The user with {emailAddress} does not exist on Azure B2C";
                return result;
            }

            if (usersListByPhone.Count == 0)
            {
                result.CanUpdate = false;
                result.ErrorMessage = $"The user with {phoneNumber} does not exist on Azure B2C";
                return result;
            }

            if (usersListByPhone[0] != usersListByEmail[0])
            {
                result.CanUpdate = false;
                result.ErrorMessage = $"The user with {phoneNumber} and user with {emailAddress} does not matched on Azure B2C";
                return result;
            }

            result.CanUpdate = true;
            result.UserId = usersListByEmail[0].Id;
            if (usersListByEmail[0].AdditionalData != null)
            {
                var patientIdObj = usersListByEmail[0].AdditionalData.Where(x => x.Key == $"extension_{ExtensionClientId.Replace("-", "")}_PatientID").FirstOrDefault();
                if (!patientIdObj.Equals(default(KeyValuePair<string, object>)))
                {
                    result.PatientId = patientIdObj.Value.ToString();
                }
                
            }
            return result;
        }

        public async Task UpdatePatientId(string userId, string patientId)
        {
            var user = new User
            {
                AdditionalData = new Dictionary<string, object>()
                {
                    { $"extension_{ExtensionClientId}_PatientID", patientId }
                }
            };
            await _graphClient.Users[userId]
                .Request()
                .UpdateAsync(user);
        }
    }
}
