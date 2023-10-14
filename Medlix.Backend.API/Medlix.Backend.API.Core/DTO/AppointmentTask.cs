using CareAPI.Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<AppointmentInputModel>(myJsonResponse);
    
    public class AppointmentRequest
    {
        public string PatientId { get; set; }
        public string? Status { get; set; }
        public List<CodingModel>? ServiceType { get; set; }
        public CodingModel? AppointmentType { get; set; }
        public string? CalendarCode { get; set; }
        public int Duration { get; set; }
        public string? Comment { get; set; }
        public bool VirtualAppointmentAllowed { get; set; }
        public string? VirtualAppointmentAppointmentType { get; set; }
        public DateTime? RequestedPeriodStart { get; set; }
        public DateTime? RequestedPeriodEnd { get; set; }
        public DateTime? ExecutionPeriodStart { get; set; }
        public DateTime? ExecutionPeriodEnd { get; set; }
    }


    public class AppointmentResponse
    {
        public AppointmentRequest Request { get; set; }
        public AppointmentData? Appointment { get; set; }
        public FhirTaskDto? Task { get; set; }
    }
}
