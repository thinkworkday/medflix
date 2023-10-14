using System;
using System.IO;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Medlix.Backend.API.Core.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Medlix.Backend.API;
using Medlix.Backend.API.BAL.AuthenticationService;
using Medlix.Backend.API.BAL.FileService;

namespace FileAPI
{
    public class UploadFile : ControllerBase
    {
        private readonly IFileService _fileService;
        private readonly IAuthenticationService _authenticationService;
        public UploadFile(IFileService fileService, IAuthenticationService authenticationService)
        {
            _fileService = fileService;
            _authenticationService = authenticationService;
        }

        /// <summary>
        /// To upload file to Azure blob storage. 
        /// File size should be less than 100 MB
        /// Only authenticated users are allowed.  
        /// Allowed files list: 
        /// ".jpeg"
        /// ".jpg"
        /// ".mp2"
        /// ".mp2v"
        /// ".mp3"
        /// ".mp4"
        /// ".mp4v"
        /// ".mpa"
        /// ".mpe"
        /// ".mpeg"
        /// ".mpf"
        /// ".mpg"
        /// ".mod"
        /// ".pdf"
        /// ".mov"
        /// ".movie"
        /// ".oga"
        /// ".ogg"
        /// ".ogv"
        /// ".aa"
        /// ".AAC"
        /// ".png"
        /// ".gif"
        /// ".ac3"
        /// ".m1v"
        /// ".m2t"
        /// ".m2ts"
        /// ".m2v"
        /// ".m3u"
        /// ".m3u8"
        /// ".m4a"
        /// ".m4b"
        /// ".m4p"
        /// ".m4r"
        /// ".m4v"
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("UploadFile")]
        public async Task<IActionResult> UploadFileAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("HTTP trigger function UploadFile processed a request.");
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);

                if (!authUser.IsAuthenticated)
                {
                    return new UnauthorizedResult();
                }
                string patientId = Convert.ToString(req.Form["patientId"]);
                if (!authUser.IsB2CToken && authUser.PatientId != patientId && patientId == "{{patientId}}" && authUser.Role == "admin")
                {
                    return new UnauthorizedResult();
                }

                DateTimeOffset now = (DateTimeOffset)DateTime.UtcNow;
                var file = req.Form.Files["file"];
                if (file == null)
                {
                    return new JsonResult("Please choose a file.");
                }
                if ((file.Length / (1024 * 1024)) > 100)
                {
                    throw new NullReferenceException("File size should be less than 100 MB.");
                }

                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                log.LogInformation("filename = " + fileName);
                string ext = Path.GetExtension(fileName);
                log.LogInformation("ext = " + ext);
                var mimeType = file.ContentType;
                log.LogInformation("mimeType = " + mimeType);

                var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                    {".jpeg", "image/jpeg"},
                    {".jpg", "image/jpeg"},
                    {".mp2", "video/mpeg"},
                    {".mp2v", "video/mpeg"},
                    {".mp3", "audio/mpeg"},
                    {".mp4", "video/mp4"},
                    {".mp4v", "video/mp4"},
                    {".mpa", "video/mpeg"},
                    {".mpe", "video/mpeg"},
                    {".mpeg", "video/mpeg"},
                    {".mpf", "application/vnd.ms-mediapackage"},
                    {".mpg", "video/mpeg"},
                    {".mod", "video/mpeg"},
                    {".pdf", "application/pdf"},
                    {".mov", "video/quicktime"},
                    {".movie", "video/x-sgi-movie"},
                    {".oga", "audio/ogg"},
                    {".ogg", "audio/ogg"},
                    {".ogv", "video/ogg"},
                    {".aa", "audio/audible"},
                    {".AAC", "audio/aac"},
                    {".png", "image/png"},
                    {".gif", "image/gif"},
                    {".ac3", "audio/ac3"},
                    {".m1v", "video/mpeg"},
                    {".m2t", "video/vnd.dlna.mpeg-tts"},
                    {".m2ts", "video/vnd.dlna.mpeg-tts"},
                    {".m2v", "video/mpeg"},
                    {".m3u", "audio/x-mpegurl"},
                    {".m3u8", "audio/x-mpegurl"},
                    {".m4a", "audio/m4a"},
                    {".m4b", "audio/m4b"},
                    {".m4p", "audio/m4p"},
                    {".m4r", "audio/x-m4r"},
                    {".m4v", "video/x-m4v"}
                };

                bool keyExists = mappings.ContainsKey(ext);
                if (!keyExists)
                {
                    return new JsonResult("Your file type is not allowed for upload");
                }
                else
                {
                    log.LogInformation(mappings[ext]);
                }

                log.LogInformation("new mimeType = " + mappings[ext]);
                mimeType = mappings[ext];

                FileUploadModel model = new()
                {
                    File = file,
                    PatientId = req.Form["patientId"],
                    FileName = Path.GetFileNameWithoutExtension(file.FileName) + "-" + now.ToString("yyyyMMddHHmmssfff") + Path.GetExtension(file.FileName),
                    ContentType = mimeType,
                };

                log.LogInformation("calling uploadService = ");
                var result = await _fileService.UploadFile(model, log);
                log.LogInformation("UploadFile HTTP triggered function executed successfully.");

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogInformation("UploadFileFuntion HTTP triggered function execution failed." + ex.Message.ToString());
                return ErrorHandler.BadRequestResult(ex, log);
            }
        }

        /// <summary>
        /// To download file from Azure blob storage. 
        /// Only authenticated users are allowed.  
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("DownloadFile")]
        public async Task<FileResult> DownloadFile([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            ResponseHeaderHandler.AddAllowOriginHeader(req);
            log.LogInformation("HTTP trigger function DownloadFile processed a request.");
            try
            {
                var authUser = await _authenticationService.AuthenticateUserAsync(req, log);

                if (!authUser.IsAuthenticated)
                {
                    log.LogInformation("unauthorized ");
                    return null;
                }
                var blobUrl = req.Query["url"].ToString();
                if (blobUrl.Substring(0, 1) != "/")
                {
                    blobUrl = "/" + blobUrl;
                }
                string patientId = blobUrl.Substring("/patient-files/".Length, 36);
                if (!authUser.IsB2CToken && authUser.PatientId != patientId && patientId == "{{patientId}}" && authUser.Role == "admin")
                {
                    log.LogInformation("unauthorized ");
                    return null;
                }

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("BlobConnectionString"));
                log.LogInformation(" blobUrl = " + blobUrl);
                log.LogInformation(" storageAccount.BlobEndpoint = " + storageAccount.BlobEndpoint);
                log.LogInformation(" storageAccount.BlobStorageUri = " + storageAccount.BlobStorageUri);

                string endpoint = storageAccount.BlobEndpoint.ToString();

                log.LogInformation(" blobUrl2 = " + blobUrl);
                log.LogInformation(endpoint.Substring(0, endpoint.Length - 1));

                blobUrl = endpoint.Substring(0, endpoint.Length - 1) + blobUrl;
                var result = await _fileService.DownloadFile(blobUrl, log);
                var fileName = blobUrl.ToString().Substring(blobUrl.ToString().LastIndexOf("/") + 1);
                log.LogInformation("UploadFile HTTP triggered function executed successfully.");
                return new FileContentResult(result.Blob.ToArray(), result.ContentType)
                {
                    FileDownloadName = fileName
                };
            }
            catch (Exception ex)
            {
                log.LogInformation("UploadFileFuntion HTTP triggered function execution failed. " + ex.Message.ToString());
                return null;
            }
        }
    }
}

