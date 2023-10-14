using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace Medlix.Backend.API.BAL.KeyVaultService
{
    public class KeyVaultService : IKeyVaultService
    {
        private static string KeyVaultURL;

        public KeyVaultService()
        {
            KeyVaultURL = Environment.GetEnvironmentVariable("KeyVaultUrl");
        }

        public async Task<string> GetSecretValue(string keyName)
        {
            string secret = string.Empty;
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            try {
                var secretBundle = await keyVaultClient.GetSecretAsync(KeyVaultURL + keyName).ConfigureAwait(false);
                secret = secretBundle.Value;
            } catch (Exception ex) {
                // log and return empty
            }
            return secret;
        }
    }
}
