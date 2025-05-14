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
            [JsonProperty("costs")]
            public MagicCardResponseCost Costs { get; set; }
        }

        public class GenerateImageRequest
        {
            public MagicCard Card { get; set; }
            public ImageGenerator.ImageGenerationOptions Options { get; set; }
            public APIKey OpenAIApiKey { get; set; }
            public APIKey ReplicateApiKey { get; set; }
            // The cost associated with generating an image for this request.
            public Cost Cost { get; set; }
        }

        public class ImageGenerationResult
        {
            public string TemporaryImageUrl { get; set; }
            public string ImageUrl { get; set; }
            public ImageGenerator.ImageGenerationOptions Options { get; set; }
            public Cost Cost { get; set; }
        }

        public class StoreCardRecordsInput
        {
            public CardResult CardResult { get; set; }
            public CardGenerationRequest Request { get; set; }
        }

        public class PrepareImageGenerationInput
        {
            public MagicCard Card { get; set; }
            public string ImageModel { get; set; }
            public APIKey OpenAIApiKey { get; set; }
            public APIKey ReplicateApiKey { get; set; }
        }

        public class UpdateUserRecordInput
        {
            public CardGenerationRequest Request { get; set; }
            public int NumberOfCardsGenerated { get; set; }
            public Cost Cost { get; set; }
        }

        public class CardContentInput
        {
            public CardGenerationRequest Request { get; set; }
            public APIKey OpenAIAPIKey { get; set; }
        }

        public class CardRequestMetadata
        {
            public string UserName { get; set; }
            public string UserSubject { get; set; }
            public string UserSuppliedApiKey { get; set; }
            public bool UserLoggedIn { get; set; }
            public string Host { get; set; }
            public string Origin { get; set; }
        }

        public const int AllowedFreeGenerationsPerDay = 5;
        public const double DefaultDailySpendLimit = 0.5;

        [Function("GenerateMagicCard")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "GET")] HttpRequestData req, [DurableClient(TaskHub = "%DurableTaskHubName%")] DurableTaskClient client, FunctionContext context)
        {
            var log = context.GetLogger<GenerateMagicCardFunction>();

            // Extract request parameters.
            var rawUserPrompt = req.GetQueryParam("userPrompt") ?? string.Empty;
            rawUserPrompt = WebUtility.UrlDecode(rawUserPrompt);
            log.LogInformation($"User prompt: {rawUserPrompt?.Replace("\n", string.Empty)}");

            var model = req.GetQueryParam("model");
            var imageModel = req.GetQueryParam("imageModel") ?? Constants.ImageModels.Dalle2ModelName;
            var includeExplanation = bool.TryParse(req.GetQueryParam("includeExplanation"), out bool result) && result;
            var generateImagePrompt = bool.TryParse(req.GetQueryParam("generateImagePrompt"), out bool hd) && hd;
            var extraCreative = bool.TryParse(req.GetQueryParam("extraCreative"), out bool ec) && ec;
            var userSuppliedApiKey = req.GetQueryParam("openAIApiKey");
            var source = req.GetQueryParam("source");

            var numCards = 1;
            if (int.TryParse(req.GetQueryParam("numCards"), out int parsedNumCards))
            {
                numCards = Math.Clamp(parsedNumCards, 1, 1);
            }

            // Extract user info from JWT token.
            var jwtToken = req.ReadJwtToken();
            var userName = Extensions.GetClaim(jwtToken, "name", defaultValue: "Anonymous");
            var userSubject = Extensions.GetClaim(jwtToken, "sub", defaultValue: "Anonymous");
            var userLoggedIn = !userSubject.Equals("Anonymous", StringComparison.OrdinalIgnoreCase);

            // If a user brings their own OpenAI API key, we need to check if they are using a valid OpenAI image model.
            if (!string.IsNullOrWhiteSpace(userSuppliedApiKey) &&
                (!imageModel.Equals(Constants.ImageModels.Dalle2ModelName) && !imageModel.Equals(Constants.ImageModels.Dalle3ModelName)))
            {
                return await req.CreateJsonResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Please select an OpenAI image model (dalle-2 or dalle-3) when bringing your own OpenAI API key."
                });
            }

            // Enforce daily spend limit for logged-in users without their own API key.
            var usersCosmosClient = new UsersClient(log);
            var user = await usersCosmosClient.GetUserRecord(userSubject);
            var spendLimitResponse = await Extensions.CheckAndEnforceDailySpendLimit(user, userSuppliedApiKey, req, log, GenerateMagicCardFunction.DefaultDailySpendLimit);
            if (spendLimitResponse != null)
            {
                return spendLimitResponse;
            }

            var requestMetadata = new CardRequestMetadata
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

            try
            {
                // 1. Retrieve API keys.
                var openAIApiKey = await context.CallActivityAsync<APIKey>(nameof(ValidateAndGetOpenAIAPIKey), request);
                var replicateApiKey = await context.CallActivityAsync<APIKey>(nameof(ValidateAndGetReplicateAPIKey), request);

                // 2. Generate card content.
                var cardResult = await context.CallActivityAsync<CardResult>(nameof(GenerateCardContent), new CardContentInput { Request = request, OpenAIAPIKey = openAIApiKey });

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
                        var imageGenerationDefaultOptions = ImageGenerator.GetImageOptionsForCard(card, request.ImageModel);

                        var generateImageRequest = new GenerateImageRequest()
                        {
                            Card = card,
                            Options = imageGenerationDefaultOptions,
                            OpenAIApiKey = openAIApiKey,
                            ReplicateApiKey = replicateApiKey,
                            Cost = new Cost()
                        };

                        // Generate detailed image prompt if requested.
                        if (request.GenerateImagePrompt)
                        {
                            // Get back the updated request with the detailed prompt and cost.
                            generateImageRequest = await context.CallActivityAsync<GenerateImageRequest>(nameof(GenerateDetailedImagePrompt), generateImageRequest);
                        }

                        // Generate image.
                        var imageResult = await context.CallActivityAsync<ImageGenerationResult>(nameof(GenerateCardImage), generateImageRequest);

                        // Update the card with image information.
                        card.TemporaryImageUrl = imageResult.TemporaryImageUrl;
                        card.ImageUrl = imageResult.ImageUrl;

                        // Merge cost information.
                        cardResult.Cost = cardResult.Cost.Merge(imageResult.Cost);
                    }
                }

                // 4. Store card records in database.
                await context.CallActivityAsync(nameof(StoreCardRecords), new StoreCardRecordsInput() { CardResult = cardResult, Request = request });

                // 5. Update user record.
                if (request.Metadata.UserLoggedIn)
                {
                    var user = await context.CallActivityAsync<User>(nameof(UpdateUserRecord), new UpdateUserRecordInput() { Request = request, NumberOfCardsGenerated = cardResult.Cards.Length, Cost = cardResult.Cost });
                    var dailySpendLimit = user.DailySpendLimit > 0 ? user.DailySpendLimit : DefaultDailySpendLimit;
                    var remainingSpendLimit = dailySpendLimit - user.TotalCostOfCardsGeneratedToday;

                    output.Costs = new MagicCardResponseCost()
                    {
                        Cost = cardResult.Cost.TotalCost,
                        UserIncurredCost = cardResult.Cost.CostToUserApiKeys,
                        ServerIncurredCost = cardResult.Cost.CostToWebsiteApiKeys,
                        UserRemainingDailySpendLimit = remainingSpendLimit,
                    };
                }

                // 6. Prepare final response.
                output.Cards = cardResult.Cards.Select(x => new MagicCardResponse(x, x.Id, includeTemporaryImage: true));
                var json = JsonConvert.SerializeObject(output);

                // 7. Log telemetry.
                var finalResponse = await context.CallActivityAsync<string>(
                    nameof(LogTelemetry),
                    new FinalTelemetryData
                    {
                        Json = json,
                        CardResult = cardResult,
                        Request = request
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

        [Function("ValidateAndGetOpenAIAPIKey")]
        public static APIKey ValidateAndGetOpenAIAPIKey([ActivityTrigger] CardGenerationRequest request, ILogger log)
        {
            var userSuppliedKey = request.Metadata.UserSuppliedApiKey;

            if (!string.IsNullOrWhiteSpace(userSuppliedKey))
            {
                return new APIKey()
                {
                    Key = userSuppliedKey,
                    IsUserSupplied = true,
                    Name = "Open AI API Key"
                };
            }

            var apiKeyToUse = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);

            return new APIKey()
            {
                Key = apiKeyToUse,
                IsUserSupplied = false,
                Name = "Open AI API Key"
            };
        }

        [Function("ValidateAndGetReplicateAPIKey")]
        public static APIKey ValidateAndGetReplicateAPIKey([ActivityTrigger] CardGenerationRequest request, ILogger log)
        {
            return new APIKey()
            {
                Key = Environment.GetEnvironmentVariable(Constants.ReplicateApiKey),
                IsUserSupplied = false,
                Name = "Replicate API Key"
            };
        }

        [Function("GenerateCardContent")]
        public static async Task<CardResult> GenerateCardContent([ActivityTrigger] CardContentInput context, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(GenerateCardContent));
            var request = context.Request;
            var apiKey = context.OpenAIAPIKey;
            return await GenerateCard.Generate(request, apiKey, log);
        }

        [Function("GenerateDetailedImagePrompt")]
        public static async Task<GenerateImageRequest> GenerateDetailedImagePrompt([ActivityTrigger] GenerateImageRequest imageOptions, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(GenerateDetailedImagePrompt));
            var incurredCost = new Cost();
            var detailedPrompt = await ImageGenerator.GenerateDetailedImagePrompt(
                imageOptions.Options,
                imageOptions.OpenAIApiKey,
                incurredCost,
                log);

            imageOptions.Options.Prompt = detailedPrompt;
            imageOptions.Cost = incurredCost;
            return imageOptions;
        }

        [Function("GenerateCardImage")]
        public static async Task<ImageGenerationResult> GenerateCardImage([ActivityTrigger] GenerateImageRequest generateImageRequest, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(GenerateCardImage));
            var stopwatch = Stopwatch.StartNew();

            var tempUrl = await ImageGenerator.GenerateImage(generateImageRequest.Options.Prompt, generateImageRequest.Options.Model, generateImageRequest.Options.Size, generateImageRequest.OpenAIApiKey, generateImageRequest.ReplicateApiKey, generateImageRequest.Cost, log);

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
                    { "imagePrompt", generateImageRequest.Options.Prompt },
                    { "imageModel", generateImageRequest.Options.Model },
                    { "imageSize", generateImageRequest.Options.Size }
                });

            return new ImageGenerationResult
            {
                TemporaryImageUrl = tempUrl,
                ImageUrl = finalUrl,
                Options = generateImageRequest.Options,
                Cost = generateImageRequest.Cost // Pass through the cost onto the return object.
            };
        }

        [Function("StoreCardRecords")]
        public static async Task StoreCardRecords([ActivityTrigger] StoreCardRecordsInput context, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(StoreCardRecords));
            var cardResult = context.CardResult;
            var request = context.Request;
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
        public static async Task<User> UpdateUserRecord([ActivityTrigger] UpdateUserRecordInput context, FunctionContext functionContext)
        {
            var log = functionContext.GetLogger(nameof(UpdateUserRecord));
            var request = context.Request;
            var cardCount = context.NumberOfCardsGenerated;
            var cost = context.Cost;

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

                var updatedUser = await usersCosmosClient.UserGeneratedCardOperation(user, request.Metadata.UserSubject, cardCount, cost);
                log.LogInformation($"Updated user record for user '{request.Metadata.UserSubject}' in database.");
                return user;
            }
            catch (Exception ex)
            {
                // Non-fatal to overall operation.
                log.LogError($"Failed to create or update user record: {ex}");
                return null;
            }
        }

        public class FinalTelemetryData
        {
            public string Json { get; set; }
            public CardResult CardResult { get; set; }
            public CardGenerationRequest Request { get; set; }
        }

        [Function("LogTelemetry")]
        public static string LogTelemetry([ActivityTrigger] FinalTelemetryData responseData, FunctionContext functionContext)
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
