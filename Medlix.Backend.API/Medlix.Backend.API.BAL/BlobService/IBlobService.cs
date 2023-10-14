
using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.BlobService
{
   public interface IBlobService
    {
        Task<string> UploadBlob(FileUploadModel model, ILogger log);
        Task<BlobDownloadModel> DownloadBlob(string url, ILogger log);
    }
}
