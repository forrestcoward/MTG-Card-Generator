using Betalgo.Ranul.OpenAI.ObjectModels;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;

namespace MTG.CardGenerator
{
    public class GenerateMagicCardFunction
    {
        public class GenerateMagicCardFunctionResponse
        {
            [JsonProperty("cards")]
            public IEnumerable<MagicCardResponse> Cards { get; set; }
        }

        public class ImageOptions
        {
            public MagicCard Card { get; set; }
            public ImageGenerator.ImageGenerationOptions Options { get; set; }
            public string ApiKey { get; set; }
        }

        public class ImageGenerationResult
        {
            public string TemporaryImageUrl { get; set; }
            public string ImageUrl { get; set; }
            public ImageGenerator.ImageGenerationOptions Options { get; set; }
        }

        public class StoreCardRecordsInput
        {
            public CardResult CardResult { get; set; }
            public CardGenerationRequest Request { get; set; }
            public string ApiKey { get; set; }
        }

        public class PrepareImageGenerationInput
        {
            public MagicCard Card { get; set; }
            public string ImageModel { get; set; }
            public string ApiKey { get; set; }
        }

        public class UpdateUserRecordInput
        {
            public CardGenerationRequest Request { get; set; }
            public int NumberOfCardsGenerated { get; set; }
            public double TotalCost { get; set; }
        }

        public class CardContentInput
        {
            public CardGenerationRequest Request { get; set; }
            public string ApiKey { get; set; }
        }

        public class RequestMetadata
        {
            public string UserName { get; set; }
            public string UserSubject { get; set; }
            public string UserSuppliedApiKey { get; set; }
            public bool UserLoggedIn { get; set; }
            public string Host { get; set; }
            public string Origin { get; set; }
        }

        private const float DefaultTemperature = 1;
        private const float CreativeTemperature = 1.3F;

        public const int AllowedFreeGenerationsPerDay = 5;

        [Function("GenerateMagicCard")]
        [FunctionAuthorize]
        public static async Task<HttpResponseData> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "GET")] HttpRequestData req, [DurableClient(TaskHub = "%DurableTaskHubName%")] DurableTaskClient client, FunctionContext context)
        {
            var log = context.GetLogger<GenerateMagicCardFunction>();

            // Extract request parameters.
            var rawUserPrompt = req.GetQueryParam("userPrompt") ?? string.Empty;
            rawUserPrompt = WebUtility.UrlDecode(rawUserPrompt);
            log.LogInformation($"User prompt: {rawUserPrompt?.Replace("\n", string.Empty)}");

            var model = req.GetQueryParam("model");
            var imageModel = req.GetQueryParam("imageModel") ?? Constants.Dalle2ModelName;
            var includeExplanation = bool.TryParse(req.GetQueryParam("includeExplanation"), out bool result) && result;
            var generateImagePrompt = bool.TryParse(req.GetQueryParam("generateImagePrompt"), out bool hd) && hd;
            var extraCreative = bool.TryParse(req.GetQueryParam("extraCreative"), out bool ec) && ec;
            var userSuppliedApiKey = req.GetQueryParam("openAIApiKey");
            var source = req.GetQueryParam("source");

            var numCards = 5;
            if (int.TryParse(req.GetQueryParam("numCards"), out int parsedNumCards))
            {
                numCards = Math.Clamp(parsedNumCards, 1, 1);
            }

            // Extract user info from JWT token.
            var jwtToken = req.ReadJwtToken();
            var userName = Extensions.GetClaim(jwtToken, "name", defaultValue: "Anonymous");
            var userSubject = Extensions.GetClaim(jwtToken, "sub", defaultValue: "Anonymous");
            var userLoggedIn = !userSubject.Equals("Anonymous", StringComparison.OrdinalIgnoreCase);

            // Check for free tier usage limits.
            if (userLoggedIn && string.IsNullOrWhiteSpace(userSuppliedApiKey))
            {
                var usersCosmosClient = new UsersClient(log);
                var user = await usersCosmosClient.GetUserRecord(userSubject);

                if (user != null && !user.IsAdmin)
                {
                    var allowedFreeCardGenerationsPerDay = user.AllowedFreeCardGenerationsPerDay == -1 ?
                        AllowedFreeGenerationsPerDay : user.AllowedFreeCardGenerationsPerDay;

                    if (user.LastActiveTime?.Date == DateTime.Now.ToUniversalTime().Date &&
                        user.NumberOfFreeCardsGeneratedToday >= allowedFreeCardGenerationsPerDay)
                    {
                        var errorMessage = $"You have exceeded your number of free generations per day ({AllowedFreeGenerationsPerDay}). Please enter your own OpenAI API key (https://platform.openai.com/api-keys) to continue generating.";
                        return await req.CreateErrorResponse(HttpStatusCode.TooManyRequests, errorMessage);
                    }
                }
            }

            var requestMetadata = new RequestMetadata
            {
                UserName = userName,
                UserSubject = userSubject,
                UserSuppliedApiKey = userSuppliedApiKey,
                UserLoggedIn = userLoggedIn,
                Host = req.Url.Host,
                Origin = req.Headers.TryGetValues("Origin", out var origins) ? origins.FirstOrDefault() : string.Empty
            };

            var request = new CardGenerationRequest
            {
                RawUserPrompt = rawUserPrompt,
                Model = model,
                ImageModel = imageModel,
                IncludeExplanation = includeExplanation,
                GenerateImagePrompt = generateImagePrompt,
                ExtraCreative = extraCreative,
                Source = source,
                NumCards = numCards,
                Metadata = requestMetadata,
            };

            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("GenerateMagicCardOrchestrator", request);
            log.LogInformation($"Started orchestration with ID '{instanceId}'.");

            // Only return the id to the client. Clients may use the GetMagicCardGenerationStatus function to query status.
            var response = req.CreateResponse(HttpStatusCode.Accepted);
            await response.WriteAsJsonAsync(new
            {
                id = instanceId
            });

            return response;
        }

        [Function("GenerateMagicCardOrchestrator")]
        public async Task<string> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            var log = context.CreateReplaySafeLogger<GenerateMagicCardFunction>();
            var request = context.GetInput<CardGenerationRequest>();
            var output = new GenerateMagicCardFunctionResponse();
            ImageGenerator.ImageGenerationOptions imageOptions = null;

            try
            {
                // 1. Validate the API key.
                var apiKey = await context.CallActivityAsync<string>(nameof(ValidateAndGetApiKey), request);
                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new Exception("Please log in or supply your own OpenAI API key in the settings to generate Magic: The Gathering cards!");
                }

                // 2. Generate card content.
                var cardResult = await context.CallActivityAsync<CardResult>(nameof(GenerateCardContent), new CardContentInput { Request = request, ApiKey = apiKey });

                if (cardResult.Cards.Length == 0)
                {
                    throw new Exception($"Unable to generate a card for the prompt '{request.RawUserPrompt}'. Your request may have been rejected as a result of the AI language model safety system.");
                }

                // 3. Generate card images if requested.
                var generateImages = request.ImageModel != "none";
                if (generateImages)
                {
                    foreach (var card in cardResult.Cards)
                    {
                        // Generate image options.
                        var options = await context.CallActivityAsync<ImageOptions>(nameof(PrepareImageGeneration), new PrepareImageGenerationInput() { Card = card, ImageModel = request.ImageModel, ApiKey = apiKey });

                        imageOptions = options.Options;

                        // Generate detailed image prompt if requested.
                        if (request.GenerateImagePrompt)
                        {
                            options = await context.CallActivityAsync<ImageOptions>(nameof(GenerateDetailedImagePrompt), options);
                            imageOptions = options.Options;
                        }

                        // Generate image.
                        var imageResult = await context.CallActivityAsync<ImageGenerationResult>(nameof(GenerateCardImage), options);

                        // Update the card with image information.
                        card.TemporaryImageUrl = imageResult.TemporaryImageUrl;
                        card.ImageUrl = imageResult.ImageUrl;
                        imageOptions = imageResult.Options;

                        // Add cost info.
                        cardResult.Cost.AddImageCost(imageOptions.Size, imageOptions.Model);
                    }
                }

                // 4. Store card records in database.
                await context.CallActivityAsync(nameof(StoreCardRecords), new StoreCardRecordsInput() { CardResult = cardResult, Request = request, ApiKey = apiKey });

                // 5. Update user record.
                if (request.Metadata.UserLoggedIn)
                {
                    await context.CallActivityAsync(nameof(UpdateUserRecord), new UpdateUserRecordInput() { Request = request, NumberOfCardsGenerated = cardResult.Cards.Length, TotalCost = cardResult.Cost.TotalCost });
                }

                // 6. Prepare final response with telemetry.
                output.Cards = cardResult.Cards.Select(x => new MagicCardResponse(x, x.Id, includeTemporaryImage: true));
                var json = JsonConvert.SerializeObject(output);

                // 7. Log telemetry
                var finalResponse = await context.CallActivityAsync<string>(
                    nameof(LogTelemetry),
                    new FinalResponseData
                    {
                        Json = json,
                        CardResult = cardResult,
                        Request = request,
                        ImageOptions = imageOptions
                    });

                context.SetCustomStatus("success");
                return json;
            }
            catch (Exception ex)
            {
                // Set the orchestration's customStatus to "error" when an exception occurs.
                var errorMessage = FormatErrorMessage(ex, request.RawUserPrompt, request.Metadata.UserSuppliedApiKey != null, request.Metadata.UserLoggedIn);
                log.LogError(ex, errorMessage);
                context.SetCustomStatus("error");
                return errorMessage;
            }
        }

        [Function("ValidateAndGetApiKey")]
        public static string ValidateAndGetApiKey([ActivityTrigger] CardGenerationRequest request, ILogger log)
        {
            var apiKeyToUse = request.Metadata.UserSuppliedApiKey;

            if (string.IsNullOrWhiteSpace(apiKeyToUse))
            {
                if (request.Metadata.UserLoggedIn)
                {
                    apiKeyToUse = Environment.GetEnvironmentVariable(Constants.OpenAIApiKeyLoggedIn);
                }

                if (string.IsNullOrWhiteSpace(apiKeyToUse))
                {
                    apiKeyToUse = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);
                }
            }

            return apiKeyToUse;
        }

        [Function("GenerateCardContent")]
        public static async Task<CardResult> GenerateCardContent([ActivityTrigger] CardContentInput context, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(GenerateCardContent));
            var request = context.Request;
            var apiKey = context.ApiKey;
            return await GenerateCard.Generate(request, apiKey, log);
        }

        [Function("PrepareImageGeneration")]
        public static ImageOptions PrepareImageGeneration([ActivityTrigger] PrepareImageGenerationInput context, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(PrepareImageGeneration));
            var options = ImageGenerator.GetImageOptionsForCard(context.Card, context.ImageModel);

            return new ImageOptions
            {
                Card = context.Card,
                Options = options,
                ApiKey = context.ApiKey
            };
        }

        [Function("GenerateDetailedImagePrompt")]
        public static async Task<ImageOptions> GenerateDetailedImagePrompt([ActivityTrigger] ImageOptions imageOptions, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(GenerateDetailedImagePrompt));
            var detailedPrompt = await ImageGenerator.GenerateDetailedImagePrompt(
                imageOptions.Options,
                imageOptions.ApiKey,
                log);

            imageOptions.Options.Prompt = detailedPrompt;
            return imageOptions;
        }

        [Function("GenerateCardImage")]
        public static async Task<ImageGenerationResult> GenerateCardImage([ActivityTrigger] ImageOptions imageOptions, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(GenerateCardImage));
            var stopwatch = Stopwatch.StartNew();

            var tempUrl = await ImageGenerator.GenerateImage(imageOptions.Options.Prompt, imageOptions.Options.Model, imageOptions.Options.Size, imageOptions.ApiKey, log);
            var finalUrl = tempUrl;

            try
            {
                var blobStorageName = Extensions.GetSettingOrThrow(Constants.BlobStorageName);
                var blobStorageEndpoint = Extensions.GetSettingOrThrow(Constants.BlobStorageEndpoint);
                var blobStorageContainerName = Extensions.GetSettingOrThrow(Constants.BlobStorageContainerName);
                var blobStorageAccessKey = Extensions.GetSettingOrThrow(Constants.BlobStorageAccessKey);

                var storeImageResult = await Extensions.StoreImageInBlobAsync(tempUrl, blobStorageName, blobStorageEndpoint, blobStorageContainerName, blobStorageAccessKey, log: log);
                finalUrl = storeImageResult.Url;
            }
            catch (Exception ex)
            {
                // Non-fatal to overall operation.
                log.LogError($"Failed to store card image in database: {ex}");
            }

            stopwatch.Stop();
            log.LogMetric("CreateImageAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                properties: new Dictionary<string, object>()
                {
                    { "imagePrompt", imageOptions.Options.Prompt },
                    { "imageModel", imageOptions.Options.Model },
                    { "imageSize", imageOptions.Options.Size }
                });

            return new ImageGenerationResult
            {
                TemporaryImageUrl = tempUrl,
                ImageUrl = finalUrl,
                Options = imageOptions.Options
            };
        }

        [Function("StoreCardRecords")]
        public static async Task StoreCardRecords([ActivityTrigger] StoreCardRecordsInput context, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(StoreCardRecords));
            var cardResult = context.CardResult;
            var request = context.Request;
            var apiKey = context.ApiKey;
            var cosmosDatabaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
            var cardsClient = new CardsClient();

            foreach (var card in cardResult.Cards)
            {
                try
                {
                    // Store the card in the database
                    var cardGenerationRecord = new CardGenerationRecord()
                    {
                        Id = card.Id,
                        GenerationMetadata = new GenerationMetaData()
                        {
                            UserPrompt = request.RawUserPrompt,
                            SystemPrompt = cardResult.SystemPrompt,
                            ImagePrompt = card.TemporaryImageUrl != null ? "Image generated" : null,
                            Model = cardResult.ActualGPTModelUsed,
                            ImageSize = request.ImageModel != "none" ? StaticValues.ImageStatics.Size.Size1024 : null,
                            ImageStyle = null,
                            ImageModel = request.ImageModel,
                            OpenAIResponse = cardResult.OpenAIResponse,
                            IncludeExplanation = request.IncludeExplanation,
                            ExtraCreative = request.ExtraCreative,
                            UserSupplied = !string.IsNullOrWhiteSpace(request.Metadata.UserSuppliedApiKey),
                            EstimatedCost = cardResult.Cost.TotalCost,
                            Timestamp = DateTime.Now.ToUniversalTime(),
                            Host = request.Metadata.Host,
                            Origin = request.Metadata.Origin
                        },
                        User = new CardUserMetadata()
                        {
                            UserName = request.Metadata.UserName,
                            UserSubject = request.Metadata.UserSubject,
                        },
                        Card = card,
                    };

                    await cardsClient.AddItemToContainerAsync(cardGenerationRecord);
                    log.LogInformation($"Wrote card generation record to database '{cosmosDatabaseId}'.");
                }
                catch (Exception ex)
                {
                    log.LogError($"Failed to store card in database: {ex}");
                }
            }
        }

        [Function("UpdateUserRecord")]
        public static async Task UpdateUserRecord([ActivityTrigger] UpdateUserRecordInput context, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(UpdateUserRecord));
            var request = context.Request;
            var cardCount = context.NumberOfCardsGenerated;
            var totalCost = context.TotalCost;

            try
            {
                var usersCosmosClient = new UsersClient(log);
                var user = await usersCosmosClient.GetUserRecord(request.Metadata.UserSubject);

                if (user == null)
                {
                    user = await usersCosmosClient.AddItemToContainerAsync(new User()
                    {
                        UserName = request.Metadata.UserName,
                        UserSubject = request.Metadata.UserSubject,
                    });
                }

                await usersCosmosClient.UserGeneratedCard(user, request.Metadata.UserSubject, cardCount, totalCost, !string.IsNullOrWhiteSpace(request.Metadata.UserSuppliedApiKey));
                log.LogInformation($"Updated user record for user '{request.Metadata.UserSubject}' in database.");
            }
            catch (Exception ex)
            {
                // Non-fatal to overall operation.
                log.LogError($"Failed to create or update user record: {ex}");
            }
        }

        public class FinalResponseData
        {
            public string Json { get; set; }
            public CardResult CardResult { get; set; }
            public CardGenerationRequest Request { get; set; }
            public ImageGenerator.ImageGenerationOptions ImageOptions { get; set; }
        }

        [Function("LogTelemetry")]
        public static string LogTelemetry([ActivityTrigger] FinalResponseData responseData, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(LogTelemetry));
            try
            {
                log?.LogInformation($"API JSON response:{Environment.NewLine}{JToken.Parse(responseData.Json)}");

                var userPromptToSubmit = $"Please generate me one Magic: The Gathering card that has the following description: {responseData.Request.RawUserPrompt}";
                if (responseData.Request.NumCards > 1)
                {
                    userPromptToSubmit = $"Please generate me {responseData.Request.NumCards} different Magic: The Gathering cards based on the following description: {responseData.Request.RawUserPrompt}";
                }

                log?.LogInformation($"Estimated cost: ${responseData.CardResult.Cost.TotalCost}");
                log?.LogMetric("GenerateMagicCard_EstimatedCost", responseData.CardResult.Cost.TotalCost,
                    new Dictionary<string, object>()
                    {
                        { "estimatedCost", responseData.CardResult.Cost.TotalCost },
                        { "imageSize", responseData.ImageOptions?.Size },
                        { "imageModel", responseData.Request.ImageModel },
                        { "generateImage", responseData.Request.ImageModel != "none" },
                        { "includeExplanation", responseData.Request.IncludeExplanation.ToString() },
                        { "generateImagePrompt", responseData.Request.GenerateImagePrompt.ToString() },
                        { "extraCreative", responseData.Request.ExtraCreative.ToString() },
                        { "model", responseData.CardResult.ActualGPTModelUsed },
                        { "systemPrompt", responseData.CardResult.SystemPrompt },
                        { "userSubject", responseData.Request.Metadata.UserSubject },
                        { "userPrompt", userPromptToSubmit },
                        { "source", responseData.Request.Source }
                    });
            }
            catch (Exception ex)
            {
                log?.LogError(ex, "Error occurred during telemetry logging");
            }

            return responseData.Json;
        }

        private static string FormatErrorMessage(Exception exception, string rawUserPrompt, bool userSuppliedKey, bool userLoggedIn)
        {
            var errorMessage = $"Error during card generation: {exception.Message}";

            if (exception.Message.ContainsIgnoreCase("Your request was rejected as a result of our safety system"))
            {
                errorMessage = $"Error: Your request to generate a card for prompt '{rawUserPrompt}' was rejected as a result of the AI language model safety system. Please try again.";
            }

            if (exception.Message.ContainsIgnoreCase("That model is currently overloaded with other requests"))
            {
                errorMessage = $"Error: Your request to generate a card for prompt '{rawUserPrompt}' failed because the AI language model is overloaded with requests. Please try again or use a different model.";
            }

            if (exception.Message.ContainsIgnoreCase("You exceeded your current quota"))
            {
                if (userSuppliedKey)
                {
                    errorMessage = $"Error: The OpenAI API key you provided has exceeded its quota. Please adjust your usage limits or increase your quota to continue using this API key.";
                }
                else if (!userSuppliedKey && userLoggedIn)
                {
                    errorMessage = $"Error: The OpenAI API key used by this website has exceeded its quota. Please set your own OpenAI API key in the settings to continue generating Magic: The Gathering cards!";
                }
                else
                {
                    errorMessage = $"Error: The OpenAI API key used by this website has exceeded its quota. Please try logging in or supplying your own OpenAI API key in the settings to continue generating Magic: The Gathering cards!";
                }
            }

            if (exception.Message.ContainsIgnoreCase("OpenAI rejected your authorization") || exception.Message.ContainsIgnoreCase("Incorrect API key provided"))
            {
                if (userSuppliedKey)
                {
                    errorMessage = $"Error: The OpenAI API key you provided is invalid. Please check the integrity of this API key.";
                }
                else if (!userSuppliedKey && userLoggedIn)
                {
                    errorMessage = $"Error: The OpenAI API key provided by this website was invalid. Please supply your own Open AI API key in the settings to continue generating Magic: The Gathering cards!";
                }
                else
                {
                    errorMessage = $"Error: Please log in or supply your own Open AI API key in the settings to generate Magic: The Gathering cards!";
                }
            }

            return errorMessage;
        }
    }
}
