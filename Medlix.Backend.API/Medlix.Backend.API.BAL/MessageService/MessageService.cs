using Medlix.Backend.API.BAL.HttpService;
using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.MessageService
{
    public class MessageService : IMessageService
    {
        private readonly IHttpService _httpService;
        public MessageService(IHttpService httpService)
        {
            _httpService = httpService;
        }

        public async Task<bool> CreateMessage(PostExtension postData, string payload, string jwt, ILogger log, string appHeader)
        {
            var url = postData.resourceType;
            if (url == null) return false;
            log.LogInformation("Body sending in post request: " + postData);
            log.LogInformation("payload: " + payload);
            log.LogInformation("url = " + "/fhir/" + url);
            log.LogInformation("appheader = " + appHeader);

            var response = await _httpService.Post("/fhir/" + url, payload, jwt, log, appHeader);
            log.LogInformation("HTTP trigger send message post response.", response);

            return response;
        }

    }
}