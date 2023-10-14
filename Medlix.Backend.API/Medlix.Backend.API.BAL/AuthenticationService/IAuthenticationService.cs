using Medlix.Backend.API.Core.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.AuthenticationService
{
    public interface IAuthenticationService
    {
        Task<AuthResult> GetJWTToken(ILogger log, string organizationId, string userId, string hashcode, string triggerDateTime, string? patientId = null, string? timeZone = null);
       
        Task<AuthUser> AuthenticateUserAsync(HttpRequest req, ILogger log);
        
        AuthResult GenerateTokenFromRefreshToken(string refreshToken);
    }
}
