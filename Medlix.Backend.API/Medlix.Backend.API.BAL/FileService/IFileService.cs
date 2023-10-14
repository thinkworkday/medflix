using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.FileService
{
    public interface IFileService
    {
        Task<string> UploadFile(FileUploadModel fileUploadModel, ILogger log);
        Task<BlobDownloadModel> DownloadFile(string url, ILogger log);
    }
}
