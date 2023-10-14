using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class Appointment
    {
        public string reference { get; set; }
    }

    public class EncounterClass
    {
        public string system { get; set; }
        public string code { get; set; }
        public string display { get; set; }
    }

    public class EncounterModel
    {

        public string resourceType { get; set; }
        public Subject subject { get; set; }
        public string status { get; set; }
        public EncounterClass encounterClass { get; set; }
        public List<Appointment> appointment { get; set; }
    }

    public class EncounterSubject
    {
        public string reference { get; set; }
    }


}
