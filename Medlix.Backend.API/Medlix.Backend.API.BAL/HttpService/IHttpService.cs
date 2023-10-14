using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.HttpService
{
    public interface IHttpService
    {
        Task<string> Get(string url);
        Task<bool> Post(string url, string data, string jwt, ILogger log, string appHeader);
        Task<bool> Put(string url, string data);
        Task<bool> Patch(string url, string data, string jwt, ILogger log, string appHeader);

    }
}
