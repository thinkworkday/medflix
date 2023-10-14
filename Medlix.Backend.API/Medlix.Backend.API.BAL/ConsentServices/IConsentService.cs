using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.BAL.ConsentServices
{
    public interface IConsentService
    {
        Task<List<CMSConsentModel>> GetConsentList(string patientId, string jwtToken, ILogger log);
        Task<FhirConsentModel> SaveConsentAsync(FhirConsentModel model, string jwtToken, ILogger log);
        Task<bool> UpdateConsentAsync(FhirConsentModel model, string consentId, string jwtToken, ILogger log);
        Task<bool> DeleteConsentAsync(string consentId, string jwtToken, ILogger log);
    }
}
