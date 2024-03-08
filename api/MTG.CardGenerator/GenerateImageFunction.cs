using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using OpenAI.ObjectModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GenerateImageFunction
    {
        /*
         * This function generates a new image for a given card and prompt. It will:
         * 1. Genenerate a new image.
         * 2. Store the image in blob storage.
         * 3. Add the image id to the card's alternative image ids. This will allow a user to see all the images they have generated for their card.
         * 
         * Users (besides admins) need to provide their own OpenAI API key in order to generate images beyond the first.
         */
        [FunctionName("GenerateNewCardImage")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> GenerateNewCardImage([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            var stopwatch = Stopwatch.StartNew();

            var userSubject = req.GetUserSubject(log);
            if (string.IsNullOrWhiteSpace(userSubject))
            {
                return new UnauthorizedResult();
            }

            var prompt = (string)req.Query["prompt"];
            var openAIApiKey = (string)req.Query["openAIApiKey"];
            var cardId = (string)req.Query["cardId"];

            if (string.IsNullOrWhiteSpace(prompt))
            {
                return new ContentResult
                {
                    StatusCode = 400,
                    // User friendly message because default prompt is empty in UI.
                    Content = $"Please specify an image prompt.",
                };
            }

            if (string.IsNullOrWhiteSpace(cardId))
            {
                return new ContentResult
                {
                    StatusCode = 400,
                    Content = $"Please specify cardId.",
                };
            }

            prompt = WebUtility.UrlDecode(prompt);
            var maxLengthPrompt = 1500;
            if (prompt.Length > maxLengthPrompt)
            {
                // Only allow maxLengthPrompt characters to prevent abuse.
                prompt = prompt[..maxLengthPrompt];
            }

            try
            {
                var usersCosmosClient = new UsersClient(log);
                var cardsClient = new CardsClient(log);

                var user = await usersCosmosClient.GetUserRecord(userSubject);
                var cardRecord = await cardsClient.GetMagicCardRecord(cardId);
                var cardHasAnImage = !string.IsNullOrWhiteSpace(cardRecord.Card.ImageUrl);

                // Regular users must provide their own key in order to generate an image beyond the first.
                if (string.IsNullOrEmpty(openAIApiKey) && cardHasAnImage && !user.IsAdmin)
                {
                    log.LogMetric("GenerateNewCardImage_Rejected", 1, properties: new Dictionary<string, object>() { { "userSubject", userSubject }, });
                    return new ContentResult
                    {
                        StatusCode = 400,
                        Content = $"Please set your own OpenAI API key in the settings to generate images beyond the first.",
                    };
                }

                // If no key is provided, use the one from the environment if the card has not had an image generated before (or the user is an admin).
                else if (string.IsNullOrEmpty(openAIApiKey) && (!cardHasAnImage || user.IsAdmin))
                {
                    openAIApiKey = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);

                    if (string.IsNullOrWhiteSpace(openAIApiKey))
                    {
                        openAIApiKey = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);
                    }
                }

                if (string.IsNullOrWhiteSpace(openAIApiKey))
                {
                    return new ContentResult
                    {
                        StatusCode = 400,
                        Content = "No OpenAI API key was found.",
                    };
                }

                var imageModel = Constants.Dalle3ModelName;
                var imageSize = StaticValues.ImageStatics.Size.Size1024;
                var url = ImageGenerator.GenerateImage(prompt, imageModel, imageSize, openAIApiKey, log).Result;

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

                var json = JsonConvert.SerializeObject(result);
                return new OkObjectResult(result);
            }
            catch (Exception exception)
            {
                if (exception.Message.ContainsIgnoreCase("OpenAI rejected your authorization") || exception.Message.ContainsIgnoreCase("Incorrect API key provided"))
                {
                    return new ContentResult
                    {
                        StatusCode = 500,
                        Content = $"Error: The OpenAI API key you provided is invalid. Please check the integrity of this API key.",
                    };
                }

                var errorMessage = $"Unexpected exception: {exception}";
                log?.LogError(exception, errorMessage);
                return new ContentResult
                {
                    StatusCode = 500,
                    Content = exception.Message,
                };
            }
        }

        [FunctionName("GetImagePrompt")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> GetImagePrompt([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                string requestBody;
                using (var streamReader = new StreamReader(req.Body))
                {
                    requestBody = await streamReader.ReadToEndAsync();
                }

                MagicCard card;
                try
                {
                    card = JsonConvert.DeserializeObject<MagicCard>(requestBody);
                }
                catch (Exception e) when (e is JsonException || e is JsonReaderException)
                {
                    var errorMessage = $"Could not parse request body to type MagicCard.";
                    log?.LogError(errorMessage);
                    return await Task.FromResult<IActionResult>(new ContentResult
                    {
                        StatusCode = 400,
                        Content = errorMessage,
                    });
                }

                var options = ImageGenerator.GetImageOptionsForCard(card, Constants.Dalle3ModelName);
                var apiKey = Environment.GetEnvironmentVariable(Constants.OpenAIApiKeyLoggedIn);
                var prompt = await ImageGenerator.GenerateDetailedImagePrompt(options, apiKey, log);

                stopwatch.Stop();
                log.LogMetric("GetImagePrompt_DurationSeconds", stopwatch.Elapsed.TotalSeconds);

                var response = new Dictionary<string, object>()
                {
                    {
                        "suggestedPrompt", prompt
                    }
                };

                var json = JsonConvert.SerializeObject(response);
                return await Task.FromResult<IActionResult>(new OkObjectResult(response));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Unexpected exception: {exception}";
                log?.LogError(exception, errorMessage);
                return await Task.FromResult<IActionResult>(new ContentResult
                {
                    StatusCode = 500,
                    Content = errorMessage,
                });
            }
        }
    }
}
