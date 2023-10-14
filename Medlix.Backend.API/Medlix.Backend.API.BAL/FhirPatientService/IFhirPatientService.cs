using CareAPI.Core.DTO;
using CareAPI.Core.DTO.Timeline;
//using Newtonsoft.Json;
using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;


namespace Medlix.Backend.API.BAL.FhirPatientService
{
    public interface IFhirPatientService
    {
        Task<bool> UpdatePatientTelecom(string patientId, string registrationId);
        Task<bool> UpdatePatientEmailPhone(string patientId, string email, string phone, string jwt, ILogger log);
        Task<PatientData> GetPatientByID(string patientId, string jwt, ILogger log);
        Task<FhirTaskStatusUpdateResponse> UpdateTaskStatus(string patientId, FhirTaskStatusUpdateRequest fhirTaskStatusUpdateRequest, string jwt);
        Task<AppointmentData> GetAppointment(string appointmentId, string jwt,ILogger logger);
        Task<AppointmentData> UpdateAppointment(AppointmentData appointmentData, string jwt,ILogger logger);

        Task<FhirTaskDto> CreateTask(FhirTaskDto fhirTaskDto, string jwt);
        Task<FhirTask> CreateTaskWithEncounterId(FhirTask fhirTaskDto, string jwt,string encounterId,ILogger logger);
        Task<EncounterResponse> CreateEncounter(string jwt,string appointmentId,string patientId,ILogger logger);

        Task<FhirTaskDto> UpdateTask(FhirTaskDto fhirTaskDto, string jwt);
        Task<FhirAppointmentTaskDto> GetTasksRelatedAppointment(string appointmentId, string jwt);
        Task<PatientSearchResult> GetPatientByIdentifier(string identifierValue, string jwt);
        Task<bool> CreateSearchParameter(string jwt);
        Task<AppointmentData?> CreateAppointment(AppointmentData appointment, string jwt);
        Task<ResponseType> SendMessage(SendMsgDto sendMsgDto, string jwt);
        Task<bool> ReadMessage(ReadMsgDto readMsgDto, string jwt);

        Task<List<dynamic>> GetPatientEverything(string patientId, string jwt);
        Task<string> CreateOrUpdatePatientEverything(List<dynamic> importDto, string jwt);

        Task<TimelineResult> GetPatientCombinedTimelineData(string patientId, string startDate, string endDate, string jwt);
    }
}
