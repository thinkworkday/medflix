using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareAPI.Core.DTO
{
   
    public class SearchParameter
    {
        public string? Id { get; set; }
        public string? ResourceType { get; set; }
        public string? Url { get; set; }
        public string? Version { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
        public string? Code { get; set; }
        public List<string>? Base { get; set; }
        public string? Type { get; set; }
        public string? Expression { get; set; }

    }

}
