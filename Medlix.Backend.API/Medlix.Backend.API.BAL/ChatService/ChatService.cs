using Medlix.Backend.API.BAL.HttpService;
using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.ChatService
{
    public class ChatService : IChatService
    {

        private readonly IHttpService _httpService;
        public ChatService(IHttpService httpService)
        {
            _httpService = httpService;
        }
        public async Task<bool> UpdateCommunication(PatchExtension patchData, string payload, string jwt, ILogger log, string appHeader)
        {
            var url = patchData.urlPatchType;
            if (url == null) return false;
            log.LogInformation("HTTP trigger send message patch request.", payload);

            var response = await _httpService.Patch("/fhir/" + url, payload, jwt, log, appHeader);
            log.LogInformation("HTTP trigger send message patch response.", response);
            return response;
        }
    }
}
