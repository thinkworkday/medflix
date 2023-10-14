using CareAPI.Core.DTO;
//using Newtonsoft.Json;

namespace Medlix.Backend.API.BAL.CmsService
{
    public interface ICmsService
    {
        Task<CmsAppointment> GetCmsAppointment(string extension, string code);
        Task<CmsWhiteList> GetCmsWhiteUserList();
    }
}
