using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Medlix.Backend.API.Core.DTO
{
    public class BlobDownloadModel
    {
        public MemoryStream Blob { get; set; }
        public string ContentType { get; set; }
    }
}
