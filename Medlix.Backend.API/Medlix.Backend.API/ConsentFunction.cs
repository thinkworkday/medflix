using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Medlix.Backend.API.BAL;
using Medlix.Backend.API.BAL.ConsentServices;
using Medlix.Backend.API.Core.DTO;
using Newtonsoft.Json.Serialization;
using Medlix.Backend.API.BAL.AuthenticationService;

namespace Medlix.Backend.API
{
    public class ConsentFunction
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IConsentService _consentService;

        public ConsentFunction(
            IConsentService consentService,
            IAuthenticationService authenticationService
        )
        {
            _authenticationService = authenticationService;
            _consentService = consentService;

        }

        /// <summary>
        /// GET, POST supported. returns ConsentList from CRM and FHIR both
        /// </summary>
        /// <param name="req">GET: patientId, jwtToken expected in queryparams</param>
        /// <param name="req">POST: patientId, jwtToken expected in req.Body</param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("Consent")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            if (req.Method.Equals("GET"))
            {
                log.LogInformation("ConsentFunction function Executing.");
                try
                {
                    string patientId = req.Query["patientId"];
                    string jwtToken = req.Query["jwtToken"];
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);
                    var result = await _consentService.GetConsentList(patientId, jwtToken, log);
                    log.LogInformation("ConsentFunction function Executed.");
                    return new OkObjectResult(result);
                }
                catch (Exception ex)
                {
                    return ErrorHandler.BadRequestResult(ex, log);
                }
            }
            if (req.Method.Equals("POST"))
            {
                log.LogInformation("ConsentFunction function Executing.");
                try
                {
                    string patientId = req.Query["patientId"];
                    string jwtToken = req.Query["jwtToken"];
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    dynamic data = JsonConvert.DeserializeObject(requestBody);
                    var result = await _consentService.GetConsentList(patientId, jwtToken, log);
                    log.LogInformation("ConsentFunction function Executed.");
                    return new OkObjectResult(result);
                }
                catch (Exception ex)
                {
                    return ErrorHandler.BadRequestResult(ex, log);
                }
            }

            return new OkObjectResult(null);
        }

        #region Consent API

        /// <summary>
        /// Creates a Consent in FHIR on the basis of request sent. 
        /// </summary>
        /// <param name="req">
        /// Sample Request [POST]: 
        /// {
        ///     "resourceType": "Consent",
        ///     "id": "consent-example-basic",
        ///     "status": "active",
        ///     "scope": {
        ///       "coding": [
        ///         {
        ///           "system": "http://terminology.hl7.org/CodeSystem/consentscope",
        ///           "code": "patient-privacy"
        ///         }
        ///       ]
        ///     },
        ///     "category": [
        ///       {
        ///         "coding": [
        ///           {
        ///             "system": "http://loinc.org",
        ///             "code": "59284-0"
        ///           }
        ///         ]
        ///       }
        ///     ],
        ///     "patient": {
        ///       "reference": "Patient/eb298f3a-85c2-4202-a84f-3982580ec8d9",
        ///       "display": "P. van de Heuvel"
        ///     }
        /// }
        /// </param>
        /// <param name="log"></param>
        /// <returns>
        /// Success: Returns the FhirConsentModel with some extra information.
        /// Error: Bad Request error will be returned if something went wrong.
        /// </returns>
        [FunctionName("SaveConsent")]
        public async Task<IActionResult> CreateConsentRecord([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            string jwtToken = req.Query["jwtToken"];
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            var data = JsonConvert.DeserializeObject<FhirConsentModel>(requestBody, serializerSettings);
            try
            {
                var responseObject = await _consentService.SaveConsentAsync(data, jwtToken, log);
                if (responseObject != null)
                {
                    return new OkObjectResult(responseObject);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return ErrorHandler.BadRequestResult(ex, log);
            }
            return ErrorHandler.BadRequestResult(new Exception("Unknown error caught. Please check logs for details."), log);
        }
        #endregion Consent API

        [FunctionName("DeleteConsent")]
        public async Task<IActionResult> DeleteConsent([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "DeleteConsent/{consentId}")] HttpRequest req, string consentId, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                if (!authUser.IsAuthenticated || authUser.Role != "admin")
                {
                    return new UnauthorizedResult();
                }
                if (string.IsNullOrEmpty(consentId))
                {
                    return new BadRequestObjectResult(new { Message = "consentId is required", Error = true });
                }
                var response = await _consentService.DeleteConsentAsync(consentId, authUser.ValidJwtToken, log);
                if (response)
                {
                    return new OkObjectResult(new { Message = "Deleted Consent on FHIR successfully" });
                }
                else
                {
                    return new BadRequestObjectResult(new { Message = "Failed Deleting consent", Error = true });
                }
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        [FunctionName("UpdateConsent")]
        public async Task<IActionResult> UpdateConsent([HttpTrigger(AuthorizationLevel.Function, "put", Route = "UpdateConsent/{consentId}")] HttpRequest req, string consentId, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var serializerSettings = new JsonSerializerSettings();
                serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                var data = JsonConvert.DeserializeObject<FhirConsentModel>(requestBody, serializerSettings);
                if (!authUser.IsAuthenticated || authUser.Role != "admin")
                {
                    return new UnauthorizedResult();
                }
                if (string.IsNullOrEmpty(consentId))
                {
                    return new BadRequestObjectResult(new { Message = "consentId is required", Error = true });
                }
                var response = await _consentService.UpdateConsentAsync(data, consentId, authUser.ValidJwtToken, log);
                if (response)
                {
                    return new OkObjectResult(new { Message = "Update Consent on FHIR successfully" });
                }
                else
                {
                    return new BadRequestObjectResult(new { Message = "Failed Updating consent", Error = true });
                }
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }
    }
}

