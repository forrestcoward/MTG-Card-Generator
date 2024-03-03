using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    internal class UploadCardImageFunction
    {
        [FunctionName("UploadCardImage")]
        [FunctionAuthorize]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var cardId = (string)req.Query["cardId"];

                var blobStorageName = Extensions.GetSettingOrThrow(Constants.BlobStorageName);
                var blobStorageEndpoint = Extensions.GetSettingOrThrow(Constants.BlobStorageEndpoint);
                var blobStorageContainerName = Extensions.GetSettingOrThrow(Constants.BlobStorageContainerName);
                var blobStorageAccessKey = Extensions.GetSettingOrThrow(Constants.BlobStorageAccessKey);

                var sharedKeyCredential = new Azure.Storage.StorageSharedKeyCredential(blobStorageName, blobStorageAccessKey);
                var blobServiceClient = new BlobServiceClient(new Uri(blobStorageEndpoint), sharedKeyCredential);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(blobStorageContainerName);

                var blobName = $"image-{cardId}.png";
                var blockBlob = blobContainerClient.GetBlockBlobClient(blobName);

                using (var stream = new MemoryStream())
                {
                    await req.Body.CopyToAsync(stream);
                    stream.Position = 0;
                    await blockBlob.UploadAsync(stream);
                }

                var uploadedBlobUrl = blockBlob.Uri.ToString();
                stopwatch.Stop();
                log.LogMetric("UploadCardImage_DurationSeconds", stopwatch.Elapsed.TotalSeconds);
                return new OkObjectResult(new { url = uploadedBlobUrl });
            }
            catch (Exception exception)
            {
                var errorMessage = $"Unexpected exception: {exception}";
                log?.LogError(exception, errorMessage);
                return new ContentResult()
                {
                    StatusCode = 500,
                    Content = errorMessage,
                };
            }
        }
    }
}
