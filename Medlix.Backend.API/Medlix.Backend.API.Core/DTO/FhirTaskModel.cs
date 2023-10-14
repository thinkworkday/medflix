namespace Medlix.Backend.API.Core.DTO
{
    public class FhirTaskOwner
    {
        public string Reference { get; set; }
    }

    public class FhirTaskModel
    {
        public string ResourceType { get; set; }
        public string Status { get; set; }
        public FhirTaskFocus focus { get; set; }
        public FhirTaskOwner Owner { get; set; }
        
    }
    public class FhirTaskFocus
    {
        public string reference { get; set; }
    }
    public class FhirTaskStatusUpdateRequest
    {
        public string TaskId { get; set; }

        public string Status { get; set; }

        public string OwnerId { get; set; }
    }

    public class FhirTaskStatusUpdateResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public dynamic UpdatedTask { get; set; }
    }
    public class FhirTaskPatchPayload
    {
        public string Op { get; set; }
        public string Path { get; set; }
        public string Value { get; set; }

    }
}
