using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareAPI.Core.DTO
{
   
    public class Meta
    {
        public string versionId { get; set; }
        public DateTime lastUpdated { get; set; }
    }

    public class PatientInformation
    {
        public string use { get; set; }
        public string family { get; set; }
        public List<string> given { get; set; }
    }

    public class Telecom
    {
        public string value { get; set; }
        public string system { get; set; }
        public string use { get; set; }
        public int? rank { get; set; }
    }

    public class PatientData
    {
        public string resourceType { get; set; }
        public string id { get; set; }
        public Meta meta { get; set; }
        public bool active { get; set; }
        public List<PatientInformation> name { get; set; }
        public List<Telecom> telecom { get; set; }
        public string gender { get; set; }
        public string birthDate { get; set; }
    }


}
