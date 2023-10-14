using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    public class Code
    {
        public List<Coding> coding { get; set; }
    }

    public class CodingTask
    {
        public string code { get; set; }
    }

    public class Encounter
    {
        public string reference { get; set; }
    }

    public class ExecutionPeriod
    {
        public string start { get; set; }
        public string end { get; set; }
    }

    public class ExtensionTask
    {
        public string url { get; set; }
        public string valueString { get; set; }
    }

    public class Owner
    {
        public string reference { get; set; }
    }

    public class FhirTask
    {
        public string resourceType { get; set; }
        public List<Extension> extension { get; set; }
        public string status { get; set; }
        public StatusReason statusReason { get; set; }
        public string intent { get; set; }
        public string priority { get; set; }
        public Code code { get; set; }
        public string description { get; set; }
        public Encounter encounter { get; set; }
        public ExecutionPeriod executionPeriod { get; set; }
        public DateTime authoredOn { get; set; }
        public DateTime lastModified { get; set; }
        public Owner owner { get; set; }
    }

    public class StatusReason
    {
        public string text { get; set; }
    }


}
