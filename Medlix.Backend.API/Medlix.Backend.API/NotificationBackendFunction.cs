using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CareAPI.BAL.NotificationHubs;
using Medlix.Backend.API;
using Medlix.Backend.API.BAL.FhirPatientService;

namespace CareAPI
{
    public class NotificationBackendFunction
    {
        private NotificationHubProxyService _notificationHubProxyService;
        private IFhirPatientService _fhirPatientService;
        
        public NotificationBackendFunction(IFhirPatientService fhirPatientService)
        {
            _notificationHubProxyService = new NotificationHubProxyService();
            _fhirPatientService = fhirPatientService;
        }

        #region
        /// <summary>
        /// Registers a device on notification hub
        /// </summary>
        
        [FunctionName("CreateRegistrationId")]
        public async Task<IActionResult> CreatePushRegistrationId([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {
                var patientId = req.Query["patientId"];
                var registrationId = await _notificationHubProxyService.CreateRegistrationId();

                //save registerartion id in DB
                var response = await _fhirPatientService.UpdatePatientTelecom(patientId, registrationId);
                return new OkObjectResult(response);
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex,log);
            }
        }

        /// <summary>
        /// Unregister device from Notification hub
        /// </summary>
        /// <param name="req"></param>
        /// <param name="registrationId"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("DeleteDeviceRegistration")]
        public async Task<IActionResult> UnregisterFromNotifications([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "DeleteDeviceRegistration/{registrationId}")] HttpRequest req, string registrationId, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                if (string.IsNullOrEmpty(registrationId))
                {
                    return new BadRequestObjectResult(new { Message = "registrationId is required", Error = true });
                }
                await _notificationHubProxyService.DeleteRegistration(registrationId);
                return new OkObjectResult(new { Message = "Deleted the device registration successfully", Error = false});
            }
            catch (Microsoft.Azure.NotificationHubs.Messaging.MessagingEntityNotFoundException ex)
            {
                return new BadRequestObjectResult(new { Message = ex.Message, Error = true });
            }
             catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex,log);
            }
        }


        [FunctionName("CreateAndUpdateDeviceRegistration")]
        public async Task<IActionResult> RegisterForPushNotifications([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] DeviceModelWithRegisterationId deviceModel, HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                if (deviceModel.Platform == MobilePlatform.notsupported)
                {
                    return new BadRequestObjectResult(new { Message = "Platform should be one of wns, apns, gcm, fcm", Error=true });
                }
                if (string.IsNullOrEmpty(deviceModel.PushChannel))
                {
                    return new BadRequestObjectResult(new { Message = "PushChannel can't be empty, please provide correct push channel", Error = true });
                }
                DeviceRegisterationResultModel registrationResult = await _notificationHubProxyService.RegisterForPushNotifications(deviceModel);

                if (registrationResult.Success)
                    return new OkObjectResult(registrationResult.RegistrationDescription);

                return new BadRequestObjectResult(new { Message = registrationResult.ErrorMessage, Error = true });
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex,log);
            }

        }

        /// <summary>
        /// Sends a notification to a device
        /// </summary>
        /// <param name="notificationRequest"></param>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("SendNotification")]
        public async Task<IActionResult> SendNotification([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] NotificationRequestModel notificationRequest, HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            try
            {
                if (notificationRequest.Platform == MobilePlatform.notsupported)
                {
                    return new BadRequestObjectResult(new { Message = "Platform should be one of wns, apns, gcm, fcm", Error = true });
                }
                if (string.IsNullOrEmpty(notificationRequest.Message))
                {
                    return new BadRequestObjectResult(new { Message = "Message can't be empty, please provide proper messsage", Error = true });
                }
                NotificationResponseModel pushDeliveryResult = await _notificationHubProxyService.SendNotification(notificationRequest);

                if (pushDeliveryResult.Success)
                    return new OkObjectResult(pushDeliveryResult.NotificationOutcome);

                return new BadRequestObjectResult(new { Message = pushDeliveryResult.ErrorMessage, Error = true });
            }
            catch (Exception ex)
            {
                return ErrorHandler.BadRequestResult(ex,log);
            }
        }

        #endregion
    }
}
