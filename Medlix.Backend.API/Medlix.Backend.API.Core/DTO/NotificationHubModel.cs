using Microsoft.Azure.NotificationHubs;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace CareAPI.BAL.NotificationHubs
{
    public class DeviceModel
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MobilePlatform Platform { get; set; }
        public string Handle { get; set; }
        public string[] Tags { get; set; }
    }

    public class DeviceModelWithRegisterationId
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MobilePlatform Platform { get; set; }
        public string Handle { get; set; }
        public string RegistrationId { get; set; }
        public string PushChannel { get; set; }
        public string[] Tags { get; set; }
    }

    public class DeviceRegisterationResultModel
    {
        public RegistrationDescription RegistrationDescription { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    public class NotificationRequestModel
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MobilePlatform Platform { get; set; }
        public string Message { get; set; }
        public string[] Tags { get; set; }
    }

    public class NotificationResponseModel
    {
        public NotificationOutcome NotificationOutcome { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }

}
