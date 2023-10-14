using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Medlix.Backend.API.Core.DTO
{
   public class FileUploadModel
    {
        public string PatientId { get; set; }
        public IFormFile File { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
