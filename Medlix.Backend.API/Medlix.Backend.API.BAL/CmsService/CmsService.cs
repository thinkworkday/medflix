using CareAPI.Core.DTO;
//using Newtonsoft.Json;
using Newtonsoft.Json;

namespace Medlix.Backend.API.BAL.CmsService
{
    public class CmsService : ICmsService
    {
        private readonly string CmsUrl = Environment.GetEnvironmentVariable("CmsUrl");


        public async Task<CmsAppointment> GetCmsAppointment(string extension, string code)
        {
            using (var client = new HttpClient())
            {
                var apiResponse = await client.GetStringAsync($"{CmsUrl}/items/appointment?filter[calendar][_eq]={extension}&filter[code][_eq]={code}&fields=*.*.*");

                var appointmentData = JsonConvert.DeserializeObject<CmsAppointment>(apiResponse);
                return appointmentData;
            }
        }

        public async Task<CmsWhiteList> GetCmsWhiteUserList()
        {
            using (var client = new HttpClient())
            {
                var apiResponse = await client.GetStringAsync($"{CmsUrl}/items/practitioner_whitelist?limit=1000");

                var cmsWhiteList = JsonConvert.DeserializeObject<CmsWhiteList>(apiResponse);
                return cmsWhiteList;
            }
        }
    }
}
