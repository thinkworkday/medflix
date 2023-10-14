using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    

    public class Class
    {
        public string system { get; set; }
        public string code { get; set; }
        public string display { get; set; }
    }

    public class MetaClass
    {
        public string versionId { get; set; }
        public DateTime lastUpdated { get; set; }
    }

    public class EncounterResponse
    {
        public string resourceType { get; set; }
        public string id { get; set; }
        public Meta meta { get; set; }
        public string status { get; set; }
        public Class @class { get; set; }
        public Subject subject { get; set; }
        public List<Appointment> appointment { get; set; }
    }

   

}
