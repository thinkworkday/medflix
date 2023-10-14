using Medlix.Backend.API.BAL.CmsService;
using Medlix.Backend.API.BAL.FhirPatientService;
using Medlix.Backend.API.Core.Common;
using Medlix.Backend.API.Core.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TimeZoneConverter;

namespace Medlix.Backend.API.BAL.AuthenticationService
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly string Audience = Environment.GetEnvironmentVariable("Audience");
        private readonly string IssUser = Environment.GetEnvironmentVariable("IssUser");
        private readonly string JwtSecretKey = Environment.GetEnvironmentVariable("JwtSecretKey");
        private string JwtRefreshSecretKey = Environment.GetEnvironmentVariable("JwtRefreshSecretKey");
        private readonly string HashSecretKey = Environment.GetEnvironmentVariable("HashSecretKey");

        private IFhirPatientService _fhirPatientService;
        private ICmsService _cmsService;

        public AuthenticationService(IFhirPatientService fhirPatientService, ICmsService cmsService)
        {
            JwtRefreshSecretKey = string.IsNullOrEmpty(JwtRefreshSecretKey) ? JwtSecretKey : JwtRefreshSecretKey;
            _fhirPatientService = fhirPatientService;
            _cmsService = cmsService;
        }

        public async Task<AuthResult> GetJWTToken(ILogger log, string organizationId, string userId, string hashcode, string triggerDateTime, string? patientId = null, string? timeZone = null)
        {
            try
            {
                DateTime currentDate = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(timeZone))
                {
                    TimeZoneInfo tzInfo;
                    try
                    {
                        tzInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZone);
                    }
                    catch
                    {
                        string tz = TZConvert.IanaToWindows(timeZone);
                        tzInfo = TimeZoneInfo.FindSystemTimeZoneById(tz);
                    }
                    currentDate = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tzInfo);
                }

                var hitDateTime = DateTime.ParseExact(triggerDateTime, "yyyyMMddHHmmssfff", null);
                var code = string.IsNullOrEmpty(patientId) ? $"{userId}|{triggerDateTime}|{HashSecretKey}|{organizationId}|X" : $"{patientId}|{userId}|{triggerDateTime}|{HashSecretKey}|{organizationId}|X";
                var computeHashCode = Unicode.ComputeSha256Hash(code);
                if (!(hitDateTime.AddMinutes(1) > currentDate))
                {
                    log.LogInformation("Returning 401 because of expired token. Max validity = 1 minute. hitDateTime = " + hitDateTime + " and currentDate = " + currentDate);
                    return new AuthResult()
                    {
                        StatusCode = 401,
                        Message = "Unauthorized no match between hitDateTime"
                    };

                }
                if (hashcode == computeHashCode)
                {
                    log.LogInformation("Hash matches hash code");

                    if (hitDateTime.Date == currentDate.Date && (currentDate - hitDateTime).TotalSeconds < 60)
                    {
                        try
                        {
                            // check if practitioner is allowed access on cms 
                            var cmsWhiteList = await _cmsService.GetCmsWhiteUserList();
                            if (cmsWhiteList?.Data?.Where(x => x.UserId == userId).Count() == 0)
                            {
                                return new AuthResult()
                                {
                                    StatusCode = 401,
                                    Message = "The user is not on the whitelist"
                                };
                            }
                        } catch(Exception ex)
                        {
                            log.LogError($"Exception triggered on if practitioner is allowed access on cms: {ex.Message}");
                        }
                        

                        Guid guidOutput;
                        if (!string.IsNullOrEmpty(patientId) && !Guid.TryParse(patientId, out guidOutput))
                        {
                            log.LogInformation($"Calling get patientId by identifier {patientId}");
                            var patientResult = await _fhirPatientService.GetPatientByIdentifier(patientId, GenerateToken(userId, organizationId, DateTime.UtcNow.AddMinutes(15), patientId));
                            if (patientResult.Entry?.Count > 0)
                            {
                                patientId = patientResult.Entry?[0]?.Resource?.Id;
                                log.LogInformation($"response of getpatientByIdentifier {patientId}");
                            } 
                            else
                            {
                                return new AuthResult()
                                {
                                    StatusCode = 401,
                                    Message = "No user found with the PatientId"
                                };
                            }

                        }

                        return new AuthResult()
                        {
                            JWTAccessToken = GenerateToken(userId, organizationId, DateTime.UtcNow.AddMinutes(15), patientId),
                            RefreshToken = GenerateToken(userId, organizationId, DateTime.UtcNow.AddHours(12), patientId, true),
                            StatusCode = 200,
                            Message = "Authorized"
                        };
                    }
                    else
                    {
                        log.LogInformation("Either no match between hitdDateimTime" + hitDateTime.Date + " and currentDate (" + currentDate.Date + ")");
                        log.LogInformation("OR: totalseconds < 60, TS=" + (currentDate - hitDateTime).TotalSeconds);
                        return new AuthResult { Message = "UnAuthorized no match between hitDateTime", StatusCode = 401 };
                    }
                }
                else
                {
                    log.LogInformation("Returning 401 because hash codes do not match");
                   
                    return new AuthResult { Message = "Unauthorized hash code does not match", StatusCode = 401 };
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Exception triggered: {ex.Message}");
                return new AuthResult { Message = $"Unauthorized {ex.Message}", StatusCode = 401 };
            }
        }
        public async Task<AuthUser> AuthenticateUserAsync(HttpRequest req, ILogger log)
        {
            AuthUser authUser = new AuthUser();

            if (!req.Headers.ContainsKey("Authorization"))
            {
                log.LogInformation("Error: Missing authorization key");
                authUser.IsAuthenticated = false;
                return authUser;
            }
            string authorizationHeader = req.Headers["Authorization"];

            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.Contains("Bearer "))
            {
                log.LogInformation("Error: Bearer keyword");
                authUser.IsAuthenticated = false;
                return authUser;
            }

            string jwtToken = authorizationHeader.Substring("Bearer ".Length);
            log.LogInformation("Checking authentiction jwtToken = " + (jwtToken.ToString().Length > 15 ? jwtToken.ToString().Substring(0, 15) : jwtToken.ToString()));
            try
            {
                var decodedToken = ValidateToken(jwtToken, JwtSecretKey);
                log.LogInformation("Decoded token succesfully");
                authUser = GetAuthUserFromDecodedToken(decodedToken);
                authUser.ValidJwtToken = jwtToken;
            }
            catch(Exception ex)
            {
                
                log.LogInformation("Not a valid custom token - checking B2C");
                log.LogInformation(ex.Message);

                // check azure b2c token validation
                try
                {
                    var decodedToken = await ValidateB2CTokenAsync(jwtToken);

                    
                    authUser = GetAuthUserFromDecodedToken(decodedToken, true);
                    authUser.ValidJwtToken = jwtToken;
                }
                catch
                {
                    log.LogInformation("Not a valid B2C token either");
                    authUser.IsAuthenticated = false;
                    authUser.ErrorMessage = "Invalid Token";
                }
            }
            return authUser;
        }
        public AuthResult GenerateTokenFromRefreshToken(string refreshToken)
        {
            try
            {
                var decodedToken = ValidateToken(refreshToken, JwtRefreshSecretKey, true);
                var patientId = decodedToken.Claims.Where(x => x.Type == "patient_id").FirstOrDefault()?.Value;
                var userId = decodedToken.Claims.Where(x => x.Type == "user_id").FirstOrDefault()?.Value;
                var organizationId = decodedToken.Issuer;

                return new AuthResult
                {
                    JWTAccessToken = GenerateToken(userId, organizationId, DateTime.UtcNow.AddMinutes(15), patientId),
                    RefreshToken = GenerateToken(userId, organizationId, DateTime.UtcNow.AddHours(12), patientId, true),
                    StatusCode = 200,
                    Message = "Authorized"
                };
            }
            catch
            {
                return new AuthResult
                {
                    StatusCode = 401,
                    Message = "UnAuthorized"
                };
            }

        }

        #region private methods
        private string GenerateToken(string userId, string organizationId, DateTime expireDuration, string? patientId = null, bool isRefreshToken = false)
        {
            var mySecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(isRefreshToken ? JwtRefreshSecretKey ?? JwtSecretKey : JwtSecretKey));
            var myIssuer = Environment.GetEnvironmentVariable("IssUser"); //organizationId;
            var myAudience = Audience;
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    string.IsNullOrEmpty(patientId) ? new Claim("role", "admin") : new Claim("patient_id", patientId),
                    new Claim("user_id", userId),
                }),
                Expires = expireDuration,
                Issuer = myIssuer,
                Audience = myAudience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private JwtSecurityToken ValidateToken(string token, string secret, bool isRefreshToken = false)
        {
            var tokenParams = new TokenValidationParameters()
            {
                RequireSignedTokens = true,
                ValidAudience = Audience,
                ValidateAudience = true,
                ValidIssuer = IssUser,
                ValidateIssuer = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = isRefreshToken,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };

            // Validate the token
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, tokenParams, out var securityToken);
            return handler.ReadJwtToken(token);
        }

        private async Task<JwtSecurityToken> ValidateB2CTokenAsync(string token)
        {
            var hostName = Environment.GetEnvironmentVariable("HostName");
            var tenantName = Environment.GetEnvironmentVariable("TenantName");
            var policyName = Environment.GetEnvironmentVariable("PolicyName");
            IdentityModelEventSource.ShowPII = true;
            var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"https://{hostName}/{tenantName}/v2.0/.well-known/openid-configuration?p={policyName}",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever()
            );
            CancellationToken ct = default;
            var discoveryDocument = await configurationManager.GetConfigurationAsync(ct);
            var signingKeys = discoveryDocument.SigningKeys;
            
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                RequireExpirationTime = false,
                RequireSignedTokens = true,
                ValidateIssuer = true,
                ValidIssuer = discoveryDocument.Issuer,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = signingKeys,
                ValidateLifetime = false,
                ValidateAudience = false,

            }, out var rawValidatedToken);
            return tokenHandler.ReadJwtToken(token);
        }

        private AuthUser GetAuthUserFromDecodedToken(JwtSecurityToken decodedToken, bool isB2CToken = false)
        {
            AuthUser authUser = new AuthUser();
            int isExpired = DateTime.Compare(decodedToken.ValidTo, DateTime.UtcNow);
            if (Environment.GetEnvironmentVariable("Env") == "dev")
            {
                // will not check expiration for dev environment
                isExpired = 1;
            }
            if (isExpired == 1)
            {
                var patientClaim = isB2CToken ? decodedToken.Claims.Where(x => x.Type == "extension_PatientID").FirstOrDefault() : decodedToken.Claims.Where(x => x.Type == "patient_id").FirstOrDefault();
                var roleClaim = decodedToken.Claims.Where(x => x.Type == "role").FirstOrDefault();

                authUser.IsAuthenticated = true;
                authUser.PatientId = patientClaim?.Value;
                authUser.Role = roleClaim?.Value;
            }
            else
            {
                authUser.IsAuthenticated = false;
                authUser.JwtTokenStatus = JwtTokenStatus.Expired;
            }
            authUser.IsB2CToken = isB2CToken;
            return authUser;
        }
        #endregion private methods

    }
}
