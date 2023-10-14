using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareAPI.Core.DTO
{
   
    public class B2CUser
    {
        public string DisplayName { get; set; }
        public string Id { get; set; }

        public string Email { get; set; }

    }

    public class B2CUpdatableResponse
    {
        public bool CanUpdate { get; set; }
        public string ErrorMessage { get; set; }
        public string UserId { get; set; }
        public string PatientId { get; set; }
    }

}
