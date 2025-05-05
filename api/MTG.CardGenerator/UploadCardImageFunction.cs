using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;

namespace MTG.CardGenerator
{
    public class UploadCardImageFunction
    {
        [Function("UploadCardImage")]
        [FunctionAuthorize]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<UploadCardImageFunction>();
            var stopwatch = Stopwatch.StartNew();

            var cardId = req.GetQueryParam("cardId");
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter 'cardId' is required.");
            }

            var blobStorageName = Extensions.GetSettingOrThrow(Constants.BlobStorageName);
            var blobStorageEndpoint = Extensions.GetSettingOrThrow(Constants.BlobStorageEndpoint);
            var blobStorageContainerName = Extensions.GetSettingOrThrow(Constants.BlobStorageContainerName);
            var blobStorageAccessKey = Extensions.GetSettingOrThrow(Constants.BlobStorageAccessKey);

            var sharedKeyCredential = new StorageSharedKeyCredential(blobStorageName, blobStorageAccessKey);
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

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new { url = uploadedBlobUrl });
            return response;
        }
    }
}
