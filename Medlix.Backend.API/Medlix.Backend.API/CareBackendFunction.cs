using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Medlix.Backend.API.BAL;
using Medlix.Backend.API.Core.DTO;
using Medlix.Backend.API.Core.Common;
using Medlix.Backend.API;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using ITfoxtec.Identity.Saml2.Schemas;
using System.Security.Authentication;
using Medlix.Backend.API.BAL.AuthenticationService;
using Medlix.Backend.API.BAL.AzureB2CService;
using Medlix.Backend.API.BAL.FhirPatientService;
using Medlix.Backend.API.BAL.SendGridService;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;

namespace CareAPI
{
    public class CareBackendFunction
    {
        private readonly IAuthenticationService _authenticationService;
        
        private readonly IAzureB2CService _azureB2CService;
        private readonly ISendGridService _smtpService;
        private IFhirPatientService _fhirPatientService;

        private static readonly TimeSpan InvitationTokenLifetime = new TimeSpan(1, 0, 0, 0);
        private string HostName = Environment.GetEnvironmentVariable("HostName");
        private string TenantName = Environment.GetEnvironmentVariable("TenantName");
        private string Nonce = Guid.NewGuid().ToString("n");
        private string OrganizationID = Environment.GetEnvironmentVariable("IssUser");
        private string ClientId = Environment.GetEnvironmentVariable("ClientId");
        private string UserId = Environment.GetEnvironmentVariable("UserId");
        private readonly string HashSecretKey = Environment.GetEnvironmentVariable("HashSecretKey");

        const string relayStateReturnUrl = "ReturnUrl";
        private readonly Saml2Configuration _config;
        private ILogger<CareBackendFunction> _log;

        public CareBackendFunction(IAuthenticationService authenticationService,
            IAzureB2CService azureB2CService,
            IFhirPatientService fhirPatientService,
            ISendGridService sendGridService,
            ILogger<CareBackendFunction> log)
        {
            _authenticationService = authenticationService;
            _azureB2CService = azureB2CService;
            _fhirPatientService = fhirPatientService;
            _smtpService = sendGridService;
            _log = log;

            try
            {
                var entityDescriptor = new EntityDescriptor();

                var ipMetaData = Environment.GetEnvironmentVariable("SamlIdPMetadata");
                var samlAppSubscriptionId = Environment.GetEnvironmentVariable("SamlAppSubscriptionId");
                var samlAppId = Environment.GetEnvironmentVariable("SamlAppId");
                if (string.IsNullOrEmpty(ipMetaData) || string.IsNullOrEmpty(samlAppSubscriptionId) || string.IsNullOrEmpty(samlAppId))
                {
                    _log.LogError("Variable `SamlIdPMetadata` is not resolved. Please check config values");
                    return;
                }
                ipMetaData = ipMetaData.Trim().Replace("{SamlAppSubscriptionId}", samlAppSubscriptionId).Replace("{SamlAppId}", samlAppId);
                log.LogInformation($"ipMetadataURl: {ipMetaData}");
                entityDescriptor.ReadIdPSsoDescriptorFromUrl(new Uri(ipMetaData));
                if (entityDescriptor.IdPSsoDescriptor == null)
                {
                    _log.LogError("`SamlIdPSsoDescriptor` is null. Please check `SamlAppSubscriptionId` and/or `SamlAppId`");
                    return;
                }
                _config = new Saml2Configuration();
                _config.Issuer = Environment.GetEnvironmentVariable("SamlIssuer");
                log.LogInformation($"Issuer as obtained from nev variable: {_config.Issuer}"); 
                _config.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
                _config.AudienceRestricted = bool.Parse(Environment.GetEnvironmentVariable("SamlAudienceRestricted")) || false;
                _config.SignatureValidationCertificates.AddRange(entityDescriptor.IdPSsoDescriptor.SigningCertificates);
                _config.SignatureAlgorithm = Environment.GetEnvironmentVariable("SamlSignatureAlgorithm") ?? "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
                _config.CertificateValidationMode = System.ServiceModel.Security.X509CertificateValidationMode.None;
                _config.RevocationMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
                _config.SignAuthnRequest = true;
                var logoutUrl = Environment.GetEnvironmentVariable("SamlSingleLogoutDestination");
                if (!string.IsNullOrEmpty(logoutUrl))
                {
                    logoutUrl = logoutUrl.Replace("{SamlAppSubscriptionId}", samlAppSubscriptionId);
                    _config.SingleLogoutDestination = new Uri(logoutUrl);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Message: {ex.Message}");
                log.LogError($"Exception: {ex.StackTrace}");
                return;
            }
        }

        /// <summary>
        /// This endpoint will Link Patient to AzureAD B2C
        /// </summary>
        /// <param name="req">GET: /PatientLink?patient_id={PATIENT_ID}&email={SOME_VALUE}&phone={SOME_VALUE}</param>
        /// <param name="log">ILogger is used to log the information or errors</param>
        /// <returns></returns>
        [FunctionName("PatientLink")]
        public async Task<IActionResult> LinkPatientToAzureAdB2C([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {

            var authUser = await _authenticationService.AuthenticateUserAsync(req, log);

            if (!authUser.IsAuthenticated)
            {
                return new UnauthorizedResult();
            }

            var patientId = req.Query["patient_id"];
            var email = req.Query["email"];
            var phone = req.Query["phone"];
            log.LogInformation("PatientLink initiated for " + patientId);
            if (string.IsNullOrEmpty(patientId) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(phone))
            {
                log.LogInformation("Generating error due to bad inputs");
                return new BadRequestObjectResult(new { Message = "patient_id, email, phone should be in query parameter", Error = true });
            }

            try
            {
                // update email and phone on FHIR record
                log.LogInformation("Trying to update data points");
                var updated = await _fhirPatientService.UpdatePatientEmailPhone(Convert.ToString(patientId), Convert.ToString(email), Convert.ToString(phone), authUser.ValidJwtToken, log);

                var canUpdateResult = await _azureB2CService.GetCanUpdateUser(Convert.ToString(email), Convert.ToString(phone));
                if (!canUpdateResult.CanUpdate)
                    return new OkObjectResult(new { Message = canUpdateResult.ErrorMessage, Error = true });

                log.LogInformation("Trying to add extenstion patientID to Azure B2C account");
                await _azureB2CService.UpdatePatientId(canUpdateResult.UserId, patientId);
                return new OkObjectResult(new { Message = "Successfully linked patient to Azure B2C account", Error = false });
            }
            catch (Exception ex)
            {
                log.LogInformation("Returning exception: " + ex.ToString());
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }


        #region Auth

        /// <summary>
        /// Authenticates a patient and generates JWT for the patient
        /// </summary>
        /// <param name="req">Parameters of hashlink will be translated from req.Body to generate the JWT</param>
        /// <param name="log">ILogger is used to log the information or errors</param>
        /// <returns></returns>
        [FunctionName("Authentication")]
        public async Task<IActionResult> Auth([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                log.LogInformation("Authentication function triggered request" + req);

                var patientId = req.Query["patient_id"];
                var dateTime = req.Query["date_time"];
                var organizationId = req.Query["organization_id"];
                var userId = req.Query["user_id"];
                var timeZone = req.Query["timeZone"];

                if (string.IsNullOrEmpty(userId))
                    userId = UserId;

                if (string.IsNullOrEmpty(organizationId))
                    organizationId = OrganizationID;

                var hashCode = req.Query["hash"];

                AuthResult authResult = await _authenticationService.GetJWTToken(
                    log,
                    Convert.ToString(organizationId),
                    Convert.ToString(userId),
                    Convert.ToString(hashCode),
                    Convert.ToString(dateTime),
                    Convert.ToString(patientId),
                    Convert.ToString(timeZone)
                );

                if (!string.IsNullOrEmpty(patientId))
                {
                    if (authResult.StatusCode == 200)
                    {
                        var action = req.Query["action"];
                        var email = req.Query["email"];
                        var phone = req.Query["phone_number"];
                        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(phone) && !string.IsNullOrEmpty(action))
                        {
                            if (Convert.ToString(action).Trim() == "link")
                            {
                                // patient link case from EHR
                                try
                                {
                                    // update email and phone on FHIR record
                                    var updated = await _fhirPatientService.UpdatePatientEmailPhone(Convert.ToString(patientId), Convert.ToString(email), Convert.ToString(phone), authResult.JWTAccessToken, log);

                                    var canUpdateResult = await _azureB2CService.GetCanUpdateUser(Convert.ToString(email), Convert.ToString(phone));
                                    if (!canUpdateResult.CanUpdate)
                                        return new BadRequestObjectResult(new { Message = canUpdateResult.ErrorMessage, Error = true });
                                    await _azureB2CService.UpdatePatientId(canUpdateResult.UserId, patientId);
                                    return new OkObjectResult(new { Message = "Successfully linked patient to Azure B2C account", Error = false });
                                }
                                catch (Exception ex)
                                {
                                    return ErrorHandler.BadRequestResult(ex, log);
                                }

                            }
                        }
                    }
                }
                log.LogInformation("Response of JWT token", authResult);
                if (authResult.StatusCode == 401)
                {
                    return new UnauthorizedObjectResult(new { Message = authResult.Message });
                }

                return new OkObjectResult(authResult);
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }

        }

        /// <summary>
        /// Check the JWT, if the role is Admin it will return `true` else `false`
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log">ILogger is used to log the information or errors</param>
        /// <returns></returns>
        [FunctionName("CheckJwtForAdmin")]
        public async Task<IActionResult> AuthForAdmin([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("CheckJwtForAdmin function execution!.");
            log.LogInformation("Request of CheckJwtForAdmin", req);

            var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
            log.LogInformation("Response of Authenticate User function ", authUser);

            
            if (authUser.IsAuthenticated && authUser.Role == "admin" && string.IsNullOrEmpty(authUser.PatientId))
                return new OkObjectResult(true);
            else
                return new OkObjectResult(false);
        }

        /// <summary>
        /// Check the JWT, if (the role is not empty) and (patientId from token matches with the patientId in JWT) it will return `true` else `false`
        /// </summary>
        /// <param name="req">GET: Expects `patient_id` in the queryparams</param>
        /// <param name="log">ILogger is used to log the information or errors</param>
        /// <returns></returns>
        [FunctionName("CheckJwtForPatient")]
        public async Task<IActionResult> AuthForPatient([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("CheckJwtForPatient function execution!.");
            log.LogInformation("Request of CheckJwtForPatient", req);

            var patientId = req.Query["patient_id"];
            var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
            log.LogInformation("Response of Authenticate User function ", authUser);

            if (authUser.IsAuthenticated && authUser.PatientId == patientId && string.IsNullOrEmpty(authUser.Role))
                return new OkObjectResult(true);
            else
                return new OkObjectResult(false);
        }

        /// <summary>
        /// This method will generate refresh token from the existing token in the request
        /// </summary>
        /// <param name="req">GET: `token` is expected in queryparams</param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("AccessToken")]
        public async Task<IActionResult> AccessToken([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                log.LogInformation("access_token function triggered request" + req);

                var token = req.Query["token"];

                if (string.IsNullOrEmpty(token))
                {
                    return new UnauthorizedResult();
                }
                var result = _authenticationService.GenerateTokenFromRefreshToken(token);
                if (result.StatusCode == 401)
                {
                    return new UnauthorizedResult();
                }
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        #endregion Auth

        #region User mobile account invitation

        [FunctionName("SendInvitation")]
        public async Task<IActionResult> Invitation([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("Send Invitation function triggered.");
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated || authUser.Role != "admin" || !string.IsNullOrEmpty(authUser.PatientId))
                {
                    return new UnauthorizedResult();
                }

                var patientEmail = req.Query["patientEmailId"];
                var patientId = req.Query["patientId"];
                var hashCode = req.Query["hash"];

                var baseUrl = req.Headers["baseUrl"];

                var patientEmailEncoded = HttpUtility.UrlEncode(patientEmail);
                var redeemUrl = UserInvitationService.GenerateSignedRedeemUrl(patientEmailEncoded, InvitationTokenLifetime, baseUrl, patientId);
                var response = await _smtpService.SendInvitationEmail(patientEmail, redeemUrl);
                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation(@$"Reedem url created{redeemUrl} and sent successfully.");
                    log.LogInformation("Send_Invitation function executed.");
                    return new OkObjectResult(new { Message = "Invitation sent successfully", StatusCode = 200 });
                }

                log.LogInformation("Send_Invitation function executed.");
                return new UnauthorizedResult();
            }

            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        [FunctionName("Redeem")]
        public async Task<IActionResult> Redeem([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                log.LogInformation("Redeem function triggered.", req);
                var emailAddress = req.Query["email"];
                var notBefore = req.Query["nbf"];
                var expires = req.Query["exp"];
                var signature = req.Query["sig"];
                var baseUrl = req.Query["baseUrl"];
                var patientId = req.Query["patientId"];

                baseUrl = baseUrl.ToString() + "/userInvitation";
                log.LogInformation("Validates the signed reedem Url");
                if (UserInvitationService.ValidateSignedRedeemUrl(emailAddress, long.Parse(notBefore), long.Parse(expires), signature, patientId))
                {
                    if (emailAddress != "")
                    {
                        log.LogInformation("Validates the signed reedem Url");
                        string displayName = req.Query["displayName"];
                        displayName = string.IsNullOrEmpty(displayName) ? " " : displayName;
                        var hostName = HostName;
                        var tenantName = TenantName;
                        var clientId = ClientId;
                        var url = UserInvitationService.GenerateSignUpUrl(hostName, tenantName, clientId, Nonce, baseUrl, emailAddress, displayName);
                        log.LogInformation(@$"Redeem function generated the signup Url {url}.");
                        log.LogInformation("Redeem function executed.");

                        return new RedirectResult(url, true);
                    }
                }

                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        [FunctionName("UserInvitation")]
        public async Task<IActionResult> InvitedUserAuthorization([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var host = req.Host.Value;
                log.LogInformation("userInvitation  function triggered.", req);
                var response = JwtTokenClaim.CheckHeaderAndJwtTokenForInviteUser(req, log);
                if (response)
                {
                    var redirectUrl = "";
                    if (host.Contains("dev"))
                    {
                        redirectUrl = "https://cockpit-dev.medlix.org/#/userInvitation";
                    }
                    else if (host.Contains("test"))
                    {
                        redirectUrl = "https://cockpit-test.medlix.org/#/userInvitation";

                    }
                    if (!string.IsNullOrEmpty(redirectUrl))
                        return new RedirectResult(redirectUrl, true);
                }
                return new UnauthorizedResult();
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        #endregion

        #region SAML SSO
        /// <summary>
        /// This endpoint will redirect the user to SSO login page (Microsoft). 
        /// </summary>
        /// <param name="req">GET: No params required</param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("SsoLogin")]
        public async Task<IActionResult> SsoLogin([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            if (_config == null)
            {
                log.LogError("SAML Configurations not found");
                return ErrorHandler.BadRequestResult(new Exception("SAML Configurations not found"), log);
            }
            var binding = new Saml2RedirectBinding();
            binding.SetRelayStateQuery(new Dictionary<string, string> { { relayStateReturnUrl, "" } });

            return binding.Bind(new Saml2AuthnRequest(_config)).ToActionResult();
        }

        /// <summary>
        /// Callback POST function for SSO Login. When the user is authenticated the claims from SSO will be sent on this endpoint to generate the JWT
        /// </summary>
        /// <param name="req">POST: receives the Claims in req.Body</param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("AssertionConsumerService")]
        public async Task<HttpResponseMessage> AssertionConsumerService([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
        {
            AuthResult retVal = new AuthResult();
            try
            {
                var binding = new Saml2PostBinding();
                var saml2AuthnResponse = new Saml2AuthnResponse(_config);

                binding.ReadSamlResponse(req.HttpContext.Request.ToGenericHttpRequest(), saml2AuthnResponse);
                if (saml2AuthnResponse.Status != Saml2StatusCodes.Success)
                {
                    throw new AuthenticationException($"SAML Response status: {saml2AuthnResponse.Status}");
                }
                binding.Unbind(req.HttpContext.Request.ToGenericHttpRequest(), saml2AuthnResponse);

                var claims = saml2AuthnResponse.ClaimsIdentity.Claims;

                log.LogInformation($"Authenticating Admin User on the basis of Claims.");

                var triggeredDateTime = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                var userId = claims
                    .FirstOrDefault(x => x.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/user_id", StringComparison.OrdinalIgnoreCase))
                    .Value;
                var organizationId = claims
                    .FirstOrDefault(x => x.Type.Equals("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/organization_id", StringComparison.OrdinalIgnoreCase))
                    .Value;

                var code = $"{userId}|{triggeredDateTime}|{HashSecretKey}|{organizationId}|X";
                var hashCode = Unicode.ComputeSha256Hash(code);

                log.LogInformation($"Attemting to generate JWT on SSO Claims...");

                retVal = await _authenticationService.GetJWTToken(log, organizationId, userId, hashCode, triggeredDateTime);
                if (retVal.StatusCode == StatusCodes.Status200OK)
                {
                    log.LogInformation($"Token Generated Successfully: ({retVal.JWTAccessToken.Substring(0, 10)}...)");
                    HttpResponseMessage resp = new HttpResponseMessage(HttpStatusCode.Moved);
                    var cookie = new CookieHeaderValue("accessToken", retVal.JWTAccessToken);
                    
                    cookie.Expires = DateTimeOffset.Now.AddDays(1);
                    cookie.Path = "/";
                    cookie.HttpOnly = false;
                    cookie.Secure = true;
                    log.LogInformation($"Setting Cookie");
                    resp.Headers.AddCookies(new CookieHeaderValue[] { cookie });

                    string frontUrl = Environment.GetEnvironmentVariable("ApiUrl");
                    log.LogInformation($"Redirect URL: { frontUrl.Split("/api")[0]}/#/patientsList");
                    resp.Headers.Location = new Uri($"{frontUrl.Split("/api")[0]}/#/patientsList");
                    return resp;
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Exception: {ex.StackTrace}");
            }
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }


        #endregion SAML SSO

        #region Evaluation for Env variable
        [FunctionName("Evaluation")]
        public async Task<IActionResult> Evaluation([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            // admin only access
            var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
            if (!authUser.IsAuthenticated || authUser.Role != "admin")
            {
                return new UnauthorizedResult();
            }
            return new OkObjectResult(new
            {
                ApiUrl= !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ApiUrl")),
                JwtSecretKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JwtSecretKey")),
                JwtRefreshSecretKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JwtRefreshSecretKey")),
                HashSecretKey = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HashSecretKey")),
                ConnectionString = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ConnectionString")),
                BlogConnectionString = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlogConnectionString")),
                HubName = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HubName")),
                Audience = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Audience")),
                IssUser = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IssUser")),
                UserId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("UserId")),
                HostName = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HostName")),
                TenantName = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TenantName")),
                PolicyName = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PolicyName")),
                Env = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("Env")),
                TenantId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TenantId")),
                ClientId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ClientId")),
                ClientSecret = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ClientSecret")),
                ExtensionClientId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ExtensionClientId")),
                CmsUrl = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CmsUrl")),
                SamlAppId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlAppId")),
                SamlAppSubscriptionId = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlAppSubscriptionId")),
                SamlIdPMetadata = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlIdPMetadata")),
                SamlSignatureAlgorithm = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlSignatureAlgorithm")),
                SamlCertificateValidationMode = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlCertificateValidationMode")),
                SamlRevocationMode = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlRevocationMode")),
                SamlAudienceRestricted = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlAudienceRestricted")),
                SamlSingleLogoutDestination = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlSingleLogoutDestination")),
                SamlCertValidityYears = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlCertValidityYears")),
                SamlAssertionConsumerService = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SamlAssertionConsumerService")),
            });
        }

        #endregion
    }
}
