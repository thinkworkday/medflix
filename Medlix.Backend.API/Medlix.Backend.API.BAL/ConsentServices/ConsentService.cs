using Medlix.Backend.API.BAL.HttpService;
using Medlix.Backend.API.Core.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SendGrid.Helpers.Errors;
using System.Net.Http.Headers;
using System.Text;

namespace Medlix.Backend.API.BAL.ConsentServices
{
    public class ConsentService : IConsentService
    {
        private static string ApiUrl;

        private readonly IHttpService _httpService;
        public ConsentService(IHttpService httpService)
        {
            _httpService = httpService;
            ApiUrl = Environment.GetEnvironmentVariable("ApiUrl");
            if (!ApiUrl.EndsWith("/"))
            {
                ApiUrl += "/";
            }
        }
        public async Task<List<CMSConsentModel>> GetConsentList(string patientId, string jwtToken, ILogger log)
        {
            try
            {
                log.LogInformation("GetConsentList method Executing.");
                string? cmsResult = await _httpService.Get(Environment.GetEnvironmentVariable("CmsUrl") + "/items/consent");
                ConsentData cmsConsentResult = JsonConvert.DeserializeObject<ConsentData>(cmsResult);
                var fhirConsentResult = await _httpService.Get($"{ApiUrl}fhir/Consent" + "?patient=" + patientId + "&status=active");
                FhirConsentModel fhirConsent = JsonConvert.DeserializeObject<FhirConsentModel>(fhirConsentResult);
                foreach (var consentData in cmsConsentResult.data)
                {
                    foreach (var entry in fhirConsent.entry)
                    {
                        var fhirCategory = entry?.resource?.category;
                        var fhirScopeCoding = entry?.resource?.scope?.coding;
                        var isSystem = fhirCategory.Any(x => x.coding.Any(y => y.system == consentData.system));
                        var isCode = fhirCategory.Any(x => x.coding.Any(y => y.code == consentData.code));
                        var isScope = fhirScopeCoding.Any(x => x.code == consentData.scope);
                        if (isSystem && isCode && isScope)
                            consentData.patient_record = entry.resource;
                    }
                }
                log.LogInformation("GetConsentList method Executed.");
                return cmsConsentResult.data;
            }
            catch (Exception ex)
            {
                log.LogError($"GetConsentList exception caugth: {ex.Message}");
                return null;
            }
        }

        public async Task<FhirConsentModel> SaveConsentAsync(FhirConsentModel fhirConsentModel, string jwtToken, ILogger log)
        {

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                string serializeData = JsonConvert.SerializeObject(fhirConsentModel, serializerSettings);

                var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync($"{ApiUrl}fhir/Consent", content);

                    var responseJson = await response.Content.ReadAsStringAsync();
                    var json = JsonConvert.DeserializeObject<FhirConsentModel>(responseJson);
                    return json;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public async Task<bool> UpdateConsentAsync(FhirConsentModel fhirConsentModel, string consentId, string jwtToken, ILogger log)
        {
            log.LogInformation("UpdateConsent method Executing.");

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                string serializeData = JsonConvert.SerializeObject(fhirConsentModel, serializerSettings);

                var content = new StringContent(serializeData, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PutAsync($"{ApiUrl}fhir/Consent/{consentId}", content);

                    log.LogInformation("UpdateConsent method Executed.");
                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    log.LogError($"UpdateConsent exception caugth: {ex.Message}");
                    return false;
                }
            }
        }

        public async Task<bool> DeleteConsentAsync(string consentId, string jwtToken, ILogger log)
        {

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                try
                {
                    var response = await client.DeleteAsync($"{ApiUrl}fhir/Consent/{consentId}");

                    var responseJson = await response.Content.ReadAsStringAsync();

                    return response.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
    }
}
