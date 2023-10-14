namespace Medlix.Backend.API.BAL.KeyVaultService
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretValue(string keyName);
    }
}
