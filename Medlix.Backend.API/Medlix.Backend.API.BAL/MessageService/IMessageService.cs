using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.MessageService
{
    public interface IMessageService
    {
        Task<bool> CreateMessage(PostExtension postData, string payload, string jwt, ILogger log, string appHeader);
    }
}
