using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareAPI.Core.DTO.Timeline
{

    public class SearchBundleResult
    {
        public string? ResourceType { get; set; }
        public string? Id { get; set; }
        public Meta? Meta { get; set; }
        public List<Link>? Link { get; set; }
        public List<dynamic>? Entry { get; set; }
    }

    public class Link
    {
        public string? Url { get; set; }
        public string? Relation { get; set; }
    }

    public class PaginationResult
    {
        public List<dynamic>? Entry { get; set; }
        public bool ExtraMore { get; set; }
        public string? ResourceType { get; set; }
    }

    public class TimelineResult
    {
        public List<dynamic>? Entry { get; set; }
        public bool ExtraAppointment { get; set; }
        public bool ExtraObservation { get; set; }
        public bool ExtraCommunication { get; set; }
        public bool ExtraQuestionnaireResponse { get; set; }
    }
}
