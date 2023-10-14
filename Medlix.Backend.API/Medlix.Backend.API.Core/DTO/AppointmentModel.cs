using Newtonsoft.Json;

namespace CareAPI.Core.DTO
{

    public class Extension
    {
        public string? Url { get; set; }
        public string? ValueString { get; set; }
    }

    public class CodeModel
    {
        public string? System { get; set; }
        public string? Code { get; set; }
        public string? Display { get; set; }
    }

    public class ReferenceModel
    {
        public string? Reference { get; set; }
    }

    public class ParticipantModel
    {
        public ReferenceModel? Actor { get; set; }
        public string? Status { get; set; }
        public string? Required { get; set; }

        public List<CodingModel> Type { get; set; }
    }

    public class CodingModel
    {
        public List<CodeModel>? Coding { get; set; }
    }

    public class ValueModel
    {
        public string? Value { get; set; }
    }

    public class TextModel
    {
        public string? Text { get; set; }
    }

    public class ExecutionPeriod
    {
        public DateTime? Start { get; set; }
    }

    public class AppointmentData
    {
        public string? ResourceType { get; set; }
        public string? Id { get; set; }
        public Meta? Meta { get; set; }
        public List<Extension>? Extension { get; set; }
        public string? Status { get; set; }
        public List<CodingModel>? ServiceType { get; set; }
        public CodingModel? AppointmentType { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
        public int MinutesDuration { get; set; }
        public List<ParticipantModel>? Participant { get; set; }
        public string? Comment { get; set; }
        public List<RequestedPeriodModel> RequestedPeriod { get; set; }
    }

    public class RequestedPeriodModel
    {
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }

    public class Entry
    {
        public string? FullUrl { get; set; }
        public FhirTaskDto? Resource { get; set; }
    }

    public class FhirAppointmentTaskDto
    {
        public string? ResourceType { get; set; }
        public string? Id { get; set; }
        public Meta? Meta { get; set; }
        public List<Entry>? Entry { get; set; }
    }
    #region FHIR Task Dto
    public class FhirTaskDto
    {
        public string? Id { get; set; }
        public DateTime? AuthoredOn { get; set; }
        public CodingModel? Code { get; set; }
        public string? Description { get; set; }
        public ReferenceModel? Encounter { get; set; }

        public ExecutionPeriod? ExecutionPeriod { get; set; }
        public List<Extension>? Extension { get; set; }
        public ReferenceModel Focus { get; set; }

        public ValueModel? GroupIdentifier { get; set; }
        public string? Intent { get; set; }
        public DateTime? LastModified { get; set; }

        public ReferenceModel? Owner { get; set; }
        public string? Priority { get; set; }
        public string? ResourceType { get; set; }
        public string? Status { get; set; }
        public TextModel? StatusReason { get; set; }
    }
    #endregion

    #region CMS Appointment Task Dto
    public class CmsTaskTypes
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public string? Name { get; set; }

        [JsonProperty("task_type")]
        public string? TaskType { get; set; }

        [JsonProperty("scheduling_amount")]
        public int? SchedulingAmount { get; set; }

        [JsonProperty("scheduling_option")]
        public string? SchedulingOption { get; set; }

        [JsonProperty("measurement_scheduling_option")]
        public string? MeasurementSchedulingOption { get; set; }
    }

    public class CmsTaskModel
    {
        public string? Id { get; set; }

        [JsonProperty("task_types_id")]
        public CmsTaskTypes? TaskTypes { get; set; }

        // **TODO**
        /*[JsonProperty("time_since_previous_execution")]
        public string? TimeSincePreviousExecution { get; set; }*/
    }

    public class CmsAppointmentData
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public string? Name { get; set; }

        public List<CmsTaskModel>? Tasks { get; set; }
    }

    public class CmsAppointment
    {
        public List<CmsAppointmentData>? Data { get; set; }
    }
    #endregion
}
