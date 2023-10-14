using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.ChatService
{
    public interface IChatService
    {
        Task<bool> UpdateCommunication(PatchExtension patchData, string payload, string jwt, ILogger log, string appHeader);
    }
}
