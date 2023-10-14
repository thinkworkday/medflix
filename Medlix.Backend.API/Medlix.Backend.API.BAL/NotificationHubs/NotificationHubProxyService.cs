using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;

namespace CareAPI.BAL.NotificationHubs
{
    public class NotificationHubProxyService
    {
        private NotificationHubClient _hubClient;

        public NotificationHubProxyService()
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionString");
            var hubName = Environment.GetEnvironmentVariable("HubName");
            _hubClient = NotificationHubClient.CreateClientFromConnectionString(connectionString, hubName, true);
        }


        /// <summary>
        /// Get registration ID from Azure Notification Hub
        /// </summary>

        public async Task<string> CreateRegistrationId()
        {
            return await _hubClient.CreateRegistrationIdAsync();
        }

        /// 
        /// <summary>
        /// Delete registration ID from Azure Notification Hub
        /// </summary>

        /// <param name="registrationId"></param>
        public async Task DeleteRegistration(string registrationId)
        {
            await _hubClient.DeleteRegistrationAsync(registrationId);
        }

        /// 
        /// <summary>
        /// Register device to receive push notifications. 
        /// Registration ID ontained from Azure Notification Hub has to be provided
        /// Then basing on platform (Android, iOS or Windows) specific
        /// handle (token) obtained from Push Notification Service has to be provided
        /// </summary>

        /// <param name="id"></param>
        /// <param name="deviceUpdate"></param>
        /// <returns></returns>
        public async Task<DeviceRegisterationResultModel> RegisterForPushNotifications(DeviceModelWithRegisterationId deviceUpdate)
        {
            RegistrationDescription registrationDescription;
            deviceUpdate.Handle = deviceUpdate.PushChannel;
            string regId;
            if (!string.IsNullOrEmpty(deviceUpdate.RegistrationId))
            {
                regId = deviceUpdate.RegistrationId.ToString();
            }
            else
            {
                regId = await CreateRegistrationId();
            }
            
            // The Device ID from the PNS
            switch (deviceUpdate.Platform)
            {
                case MobilePlatform.wns:
                    registrationDescription = new WindowsRegistrationDescription(deviceUpdate.Handle);
                    break;
                case MobilePlatform.apns:
                    registrationDescription = new AppleRegistrationDescription(deviceUpdate.Handle);
                    break;
                case MobilePlatform.gcm:
                    registrationDescription = new GcmRegistrationDescription(deviceUpdate.Handle);
                    break;
                default :
                    registrationDescription = new FcmRegistrationDescription(deviceUpdate.Handle);
                    break;
            }

            registrationDescription.RegistrationId = regId;
            if (deviceUpdate.Tags != null)
                registrationDescription.Tags = new HashSet<string>(deviceUpdate.Tags);

            try
            {
                var result = await _hubClient.CreateOrUpdateRegistrationAsync(registrationDescription);
                return new DeviceRegisterationResultModel
                {
                    RegistrationDescription = result,
                    Success = true
                };
            }
            catch (MessagingException ex)
            {
                return new DeviceRegisterationResultModel
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
       
        /// 
        /// <summary>
        /// Send push notification to specific platform (Android, iOS or Windows)
        /// </summary>

        /// <param name="newNotification"></param>
        /// <returns></returns>
        public async Task<NotificationResponseModel> SendNotification(NotificationRequestModel notificationRequest)
        {
            try
            {
                NotificationOutcome outcome = null;
                NotificationResponseModel response = new NotificationResponseModel();

                switch (notificationRequest.Platform)
                {
                    case MobilePlatform.wns:
                        // Windows 8.1 / Windows Phone 8.1
                        var toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">" + notificationRequest.Message + "</text></binding></visual></toast>";
                        if (notificationRequest.Tags == null)
                            outcome = await _hubClient.SendWindowsNativeNotificationAsync(toast);
                        else
                            outcome = await _hubClient.SendWindowsNativeNotificationAsync(toast, notificationRequest.Tags);
                        break;
                    case MobilePlatform.apns:
                        // iOS
                        var alert = "{\"aps\":{\"alert\":\"" + notificationRequest.Message + "\"}}";
                        if (notificationRequest.Tags == null)
                            outcome = await _hubClient.SendAppleNativeNotificationAsync(alert);
                        else
                            outcome = await _hubClient.SendAppleNativeNotificationAsync(alert, notificationRequest.Tags);
                        break;
                    case MobilePlatform.fcm:
                        // Android
                        var notif = "{ \"data\" : {\"message\":\"" + notificationRequest.Message + "\"}}";
                        if (notificationRequest.Tags == null)
                            outcome = await _hubClient.SendFcmNativeNotificationAsync(notif);
                        else
                            outcome = await _hubClient.SendFcmNativeNotificationAsync(notif, notificationRequest.Tags);
                        break;
                }

                if (outcome != null)
                {
                    if (!((outcome.State == NotificationOutcomeState.Abandoned) || (outcome.State == NotificationOutcomeState.Unknown)))
                    {
                        response.Success = true;
                        response.NotificationOutcome = outcome;
                        return response;
                    }
                        
                }
                response.Success = false;
                response.ErrorMessage = "Notification was not sent due to issue. Please send again.";
                return response;
            }

            catch (MessagingException ex)
            {
                return new NotificationResponseModel
                {
                    ErrorMessage = ex.Message,
                    Success = false,
                };
            }

            catch (ArgumentException ex)
            {
                return new NotificationResponseModel
                {
                    ErrorMessage = ex.Message,
                    Success = false,
                };
            }
        }



    }
}
