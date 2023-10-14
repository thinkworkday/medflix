using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{

    [JsonObject("data")]
    public class CMSConsentModel
    {
        public int? id { get; set; }
        public string? status { get; set; }
        public object? sort { get; set; }
        public string? user_created { get; set; }
        public DateTime? date_created { get; set; }
        public string? user_updated { get; set; }
        public DateTime? date_updated { get; set; }
        public string? title { get; set; }
        public string? subtitle { get; set; }
        public string? description { get; set; }
        public string? identifier { get; set; }
        public int? provider { get; set; }
        public int? consent_category { get; set; }
        public string? intro_text { get; set; }
        public string? name { get; set; }
        public string? video_url { get; set; }
        public string? duration { get; set; }
        public bool? default_visible { get; set; }
        public string? confirmation_question { get; set; }
        public string? system { get; set; }
        public string? code { get; set; }
        public string? scope { get; set; }
        public dynamic? patient_record { get; set; }
    }

    [JsonObject("Root")]
    public class ConsentData
    {
        [JsonProperty("data")]
        public List<CMSConsentModel> data { get; set; }
    }
}

