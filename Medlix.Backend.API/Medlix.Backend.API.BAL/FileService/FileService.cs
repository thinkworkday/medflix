using Medlix.Backend.API.BAL.BlobService;
using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Logging;

namespace Medlix.Backend.API.BAL.FileService
{
    public class FileService : IFileService
    {
        private readonly IBlobService _blobService;
        public FileService(IBlobService blobService)
        {
            _blobService = blobService;

        }

        public async Task<BlobDownloadModel> DownloadFile(string url, ILogger log)
        {
            return await _blobService.DownloadBlob(url, log);
        }

        public async Task<string> UploadFile(FileUploadModel fileUploadModel, ILogger log)
        {
            return await _blobService.UploadBlob(fileUploadModel, log);
        }
    }
}
