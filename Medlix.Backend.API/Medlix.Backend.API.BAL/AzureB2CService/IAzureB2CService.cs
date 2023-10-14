using Microsoft.Graph;
using Azure.Identity;
using CareAPI.Core.DTO;

namespace Medlix.Backend.API.BAL.AzureB2CService
{
    public interface IAzureB2CService
    {
        Task<B2CUpdatableResponse> GetCanUpdateUser(string emailAddress, string phoneNumber);
        Task UpdatePatientId(string userId, string patientId);
    }
}
