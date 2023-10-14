using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareAPI.Core.DTO
{
   
    public class CmsWhiteList
    {
        [JsonProperty("data")]
        public List<CmsWhiteUser>? Data { get; set; }
    }
    public class CmsWhiteUser
    {
        public string? Id { get; set; }
        public string? Comment { get; set; }

        [JsonProperty("user_id")]
        public string? UserId { get; set; }
        public string? Status { get; set; }
    }
    
}
