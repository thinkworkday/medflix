using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    public class FeedbackDto
    {
        public string? PatientId { get; set; }
        public string? Email { get; set; }
        public string? Message { get; set; }
    }
}
