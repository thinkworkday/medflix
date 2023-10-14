using Medlix.Backend.API.Core.DTO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Medlix.Backend.API.BAL.BlobService
{
    public class BlobService : IBlobService
    {
        private readonly string _containerName = "patient-files";
        private readonly string _blobStorageConnectionString = "";
        public BlobService(IConfiguration configuration)
        {
            _blobStorageConnectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
        }

        public async Task<BlobDownloadModel> DownloadBlob(string url, ILogger log)
        {
            try
            {
                log.LogInformation("executing DownloadBlob method!");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_blobStorageConnectionString);
                var blob = new CloudBlockBlob(new Uri(url), cloudStorageAccount.Credentials);
                BlobDownloadModel blobModel = new BlobDownloadModel();
                MemoryStream ms = new MemoryStream();

                await blob.DownloadToStreamAsync(ms);
                blobModel.Blob = ms;
                blobModel.ContentType = blob.Properties.ContentType;
                return blobModel;
            }
            catch (Exception ex)
            {
                log.LogInformation(" DownloadBlob method Exception:"+ ex.Message);
                return null;
            }
        }

        public async Task<string> UploadBlob(FileUploadModel fileUploadModel, ILogger log)
        {
            try
            {
                log.LogInformation("UploadBlob called");
                CloudStorageAccount cloudStorageAccount = CloudStorageAccount.Parse(_blobStorageConnectionString);
                CloudBlobClient cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(_containerName);
                CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference($"{fileUploadModel.PatientId}/{fileUploadModel.FileName}");

                log.LogInformation("Setting permission");
                if (await cloudBlobContainer.CreateIfNotExistsAsync())
                {
                    await cloudBlobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });
                }

                if (fileUploadModel.File.FileName != null)
                {
                    cloudBlockBlob.Properties.ContentType = fileUploadModel.ContentType;
                    await cloudBlockBlob.UploadFromStreamAsync(fileUploadModel.File.OpenReadStream());
                }
                log.LogInformation(cloudBlockBlob.Uri.ToString());
                var url = new Uri(cloudBlockBlob.Uri.ToString());
                log.LogInformation(url.PathAndQuery);

                return url.PathAndQuery;
            }
            catch(Exception ex)
            {
                log.LogInformation(" DownloadBlob method Exception:" + ex.Message);
                return null;
            }
        }
    }
}
