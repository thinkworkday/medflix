using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    public class FhirConsentModel
    {
        public string resourceType { get; set; }
        public string id { get; set; }
        public Meta meta { get; set; }
        public string type { get; set; }
        public List<Link> link { get; set; }
        public List<Entry> entry { get; set; }

        public string status { get; set; }
        public Scope scope { get; set; }
        public List<Category> category { get; set; }
        public PatientDto patient { get; set; }
    }

    public class PatientDto
    {
        public string reference { get; set; }
        public string display { get; set; }
    }

    public class Scope
    {
        public List<Coding> coding { get; set; }
    }
    public class Category
    {
        public List<Coding> coding { get; set; }
    }
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Meta
    {
        public DateTime lastUpdated { get; set; }
        public string versionId { get; set; }
    }

    public class Link
    {
        public string relation { get; set; }
        public string url { get; set; }
    }

    public class Coding
    {
        public string system { get; set; }
        public string code { get; set; }

        public static implicit operator List<object>(Coding v)
        {
            throw new NotImplementedException();
        }
    }

    public class Patient
    {
        public string reference { get; set; }
    }

    public class Provision
    {
        public string type { get; set; }
    }

    public class Resource
    {
        public string resourceType { get; set; }
        public string id { get; set; }
        public Meta meta { get; set; }
        public string status { get; set; }
        public Scope scope { get; set; }
        public List<Category> category { get; set; }
        public Patient patient { get; set; }
        public string dateTime { get; set; }
        public Provision provision { get; set; }
    }

    public class Search
    {
        public string mode { get; set; }
    }

    public class Entry
    {
        public string fullUrl { get; set; }
        public Resource resource { get; set; }
        public Search search { get; set; }
    }




}
