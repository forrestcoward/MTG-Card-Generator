using Betalgo.Ranul.OpenAI.ObjectModels;
using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

namespace MTG.CardGenerator
{
    public class GenerateImageFunction
    {
        /*
         * This function generates a new image for a given card and prompt. It will:
         * 1. Generate a new image.
         * 2. Store the image in blob storage.
         * 3. Add the image ID to the card's alternative image IDs. This will allow a user to see all the images they have generated for their card.
         *
         * Users (besides admins) need to provide their own OpenAI API key in order to generate images beyond the first.
         */
        [Function("GenerateNewCardImage")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> GenerateNewCardImage([HttpTrigger(AuthorizationLevel.Anonymous, "GET")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<GenerateImageFunction>();
            var stopwatch = Stopwatch.StartNew();

            var userSubject = req.GetUserSubject(log);
            if (string.IsNullOrWhiteSpace(userSubject))
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var prompt = req.GetQueryParam("prompt");
            var cardId = req.GetQueryParam("cardId");

            // Extract OpenAI API key from header instead of query parameter for security
            var userSuppliedOpenAIAPIKey = req.Headers.TryGetValues("x-openai-api-key", out var apiKeyValues)
                ? apiKeyValues.FirstOrDefault()
                : req.GetQueryParam("openAIApiKey"); // Fallback to query param for backward compatibility.

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Please specify an image prompt.");
            }

            if (string.IsNullOrWhiteSpace(cardId))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Please specify cardId.");
            }

            prompt = WebUtility.UrlDecode(prompt);
            var maxLengthPrompt = 1500;
            if (prompt.Length > maxLengthPrompt)
            {
                prompt = prompt[..maxLengthPrompt]; // Truncate to prevent abuse.
            }

            var usersCosmosClient = new UsersClient(log);
            var cardsClient = new CardsClient(log);

            var user = await usersCosmosClient.GetUserRecord(userSubject);
            var cardRecord = await cardsClient.GetMagicCardRecord(cardId);
            var cardHasAnImage = !string.IsNullOrWhiteSpace(cardRecord.Card.ImageUrl);

            // If user doesn't provide their own OpenAI API key, charge the server and use a replicate model.
            // If the user provides their own OpenAI API key, use that with DALL-E 3. Server incurs no cost.
            var userSuppliedOpenAIApiKey = false;
            var openAIApiKey = string.Empty;
            var imageModel = Constants.ImageModels.GoogleImagen4FastModelName;

            if (!string.IsNullOrWhiteSpace(userSuppliedOpenAIAPIKey))
            {
                userSuppliedOpenAIApiKey = true;
                openAIApiKey = userSuppliedOpenAIAPIKey;
                imageModel = Constants.ImageModels.Dalle3ModelName;
            }

            // Enforce daily spend limit for logged-in users without their own API key.
            if (!userSuppliedOpenAIApiKey)
            {
                var spendLimitResponse = await Extensions.CheckAndEnforceDailySpendLimit(user, req, log, GenerateMagicCardFunction.DefaultDailySpendLimit);
                if (spendLimitResponse != null)
                {
                    return spendLimitResponse;
                }
            }

            var imageSize = StaticValues.ImageStatics.Size.Size1024;
            var incurredCost = new Cost();

            var openAIAPIKey = new APIKey()
            {
                Key = openAIApiKey,
                Name = "Open AI API Key",
                IsUserSupplied = userSuppliedOpenAIApiKey
            };

            var replicateAPIKey = new APIKey()
            {
                Key = Environment.GetEnvironmentVariable(Constants.ReplicateApiKey),
                Name = "Replicate API Key",
                IsUserSupplied = false
            };

            var url = await ImageGenerator.GenerateImage(prompt, imageModel, StaticValues.ImageStatics.Size.Size1024, openAIAPIKey, replicateAPIKey, incurredCost, log);

            var blobStorageName = Extensions.GetSettingOrThrow(Constants.BlobStorageName);
            var blobStorageEndpoint = Extensions.GetSettingOrThrow(Constants.BlobStorageEndpoint);
            var blobStorageContainerName = Extensions.GetSettingOrThrow(Constants.BlobStorageContainerName);
            var blobStorageAccessKey = Extensions.GetSettingOrThrow(Constants.BlobStorageAccessKey);
            var storeImageResult = await Extensions.StoreImageInBlobAsync(url, blobStorageName, blobStorageEndpoint, blobStorageContainerName, blobStorageAccessKey, log: log);

            // The first time a user generates an image for a card, set that as the card's image, otherwise add as an alternate.
            if (string.IsNullOrWhiteSpace(cardRecord.Card.ImageUrl))
            {
                await cardsClient.SetCardImageUrl(cardRecord, storeImageResult.Url);
            }
            else
            {
                await cardsClient.AddAlternativeImageId(cardRecord, storeImageResult.ImageId.ToString());
            }

            await usersCosmosClient.UserGeneratedImageOperation(user, userSubject, incurredCost);

            log.LogMetric("GenerateNewCardImage_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                properties: new Dictionary<string, object>()
                {
                    { "imagePrompt", prompt },
                    { "imageModel", imageModel },
                    { "imageSize", imageSize },
                    { "userSubject", userSubject },
                });

            var result = new Dictionary<string, object>()
            {
                { "imageUrl", storeImageResult.Url },
                { "temporaryUrl", url }
            };

            return await req.CreateJsonResponse(HttpStatusCode.OK, result);
        }

        [Function("GetImagePrompt")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> GetImagePrompt([HttpTrigger(AuthorizationLevel.Anonymous, "POST")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<GenerateImageFunction>();
            var stopwatch = Stopwatch.StartNew();

            string requestBody;
            using (var streamReader = new StreamReader(req.Body))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            UICard card;
            try
            {
                card = JsonConvert.DeserializeObject<UICard>(requestBody);
            }
            catch (JsonException)
            {
                var errorMessage = "Could not parse request body to type MagicCard.";
                log.LogError(errorMessage);
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, errorMessage);
            }

            var options = ImageGenerator.GetImageOptionsForCard(card, Constants.ImageModels.Dalle3ModelName);
            var openAIApiKey = new APIKey()
            {
                Key = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey),
                Name = "Open AI API Key",
                IsUserSupplied = false
            };

            var incurredCost = new Cost();
            var prompt = await ImageGenerator.GenerateDetailedImagePrompt(options, openAIApiKey, incurredCost,log);

            stopwatch.Stop();
            log.LogMetric("GetImagePrompt_DurationSeconds", stopwatch.Elapsed.TotalSeconds);

            var response = new Dictionary<string, object>()
            {
                { "suggestedPrompt", prompt }
            };

            return await req.CreateJsonResponse(HttpStatusCode.OK, response);
        }
    }
}
