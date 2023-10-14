using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareAPI.Core.DTO
{
   
    public class PatientSearchResult
    {
        public string? ResourceType { get; set; }
        public string? Id { get; set; }
        public Meta? Meta { get; set; }
        public List<PatientEntry>? Entry { get; set; }
    }
    public class PatientResource
    {
        public string? Id { get; set; }
        public string? ResourceType { get; set; }
        public Meta? Meta { get; set; }
    }
    public class PatientEntry
    {
        public string? FullUrl { get; set; }
        public PatientResource? Resource { get; set; }
    }

    public class PatientEverythingResult
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
}
