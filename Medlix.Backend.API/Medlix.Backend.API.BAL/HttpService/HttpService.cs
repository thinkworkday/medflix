using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.HttpService
{
    public class HttpService : IHttpService
    {
        private string ApiUrl;

        public HttpService()
        {
            ApiUrl = Environment.GetEnvironmentVariable("ApiUrl");
            if (!string.IsNullOrEmpty(ApiUrl) && !ApiUrl.EndsWith("/"))
            {
                ApiUrl += "/";
            }
        }

        public async Task<string> Get(string url)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync(url);
                    return response;
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<bool> Post(string url, string data, string jwt, ILogger log, string appHeader)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    if (appHeader == "Cockpit")
                    {
                        client.DefaultRequestHeaders.Add("X-app", "Cockpit");
                    }
                    var requestUri = ApiUrl + url;
                    log.LogInformation("requestUri=" + requestUri);
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(requestUri, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    log.LogInformation("success=" + response.IsSuccessStatusCode);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> Put(string url, string data)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var requestUri = ApiUrl + url;
                    var content = new StringContent(data, Encoding.UTF8, "application/json");
                    var response = await client.PutAsync(requestUri, content);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> Patch(string url, string data, string jwt, ILogger log, string appHeader)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                    if (appHeader == "Cockpit")
                    {
                        client.DefaultRequestHeaders.Add("X-app", "Cockpit");
                    }
                    var requestUri = ApiUrl + url;
                    log.LogInformation("requestUri=" + requestUri);

                    var content = new StringContent(data, Encoding.UTF8, "application/json-patch+json");
                    log.LogInformation("content=" + content);
                    var response = await client.PatchAsync(requestUri, content);
                    log.LogInformation("success=" + response.IsSuccessStatusCode);
                    log.LogInformation("response=" + response.Content.ToString());
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

    }
}
