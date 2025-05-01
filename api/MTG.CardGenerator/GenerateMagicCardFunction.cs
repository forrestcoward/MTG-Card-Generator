using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Managers;
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
using System.Text.RegularExpressions;

namespace MTG.CardGenerator
{
    public class GenerateMagicCardFunction
    {
        private class OpenAIMagicCardResponse
        {
            public BasicCard[] Cards { get; set; }
        }

        private class GenerateMagicCardFunctionResponse
        {
            [JsonProperty("cards")]
            public IEnumerable<MagicCardResponse> Cards { get; set; }
        }

        public class CardGenerationRequest
        {
            public string RawUserPrompt { get; set; }
            public string Model { get; set; }
            public string ImageModel { get; set; }
            public bool IncludeExplanation { get; set; }
            public bool GenerateImagePrompt { get; set; }
            public bool ExtraCreative { get; set; }
            public string UserSuppliedApiKey { get; set; }
            public string Source { get; set; }
            public int NumCards { get; set; }
            public string UserName { get; set; }
            public string UserSubject { get; set; }
            public string Host { get; set; }
            public string Origin { get; set; }
            public bool UserLoggedIn { get; set; }
        }

        public class CardResult
        {
            public MagicCard[] Cards { get; set; }
            public string OpenAIResponse { get; set; }
            public float Temperature { get; set; }
            public int TokensUsed { get; set; }
            public string ActualGPTModelUsed { get; set; }
            public string SystemPrompt { get; set; }
            public Cost Cost { get; set; }
            public int AttemptsToGenerateCard { get; set; }
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

        const string GenerateCreativeCardSystemPrompt = $@"
You are an assistant who works as a Magic: The Gathering card designer. The cards you generate should be as creative as possible and utilize Magic: The Gathering card mechanics in unique and interesting ways, ideally mixing multiple mechanics and keywords together in novel ways and have good synergy with other cards or archetypes. The cards you are known to generate elicit a 'wow, cool, that's a great idea!' response from players and leave them surprised by the card that generated. Remember, be extra creative!
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', and 'rarity'.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const string GenerateCreativeCardSystemPromptWithExplanation = $@"
You are an assistant who works as a Magic: The Gathering card designer. The cards you generate should be as creative as possible and utilize Magic: The Gathering card mechanics in unique and interesting ways, ideally mixing multiple mechanics and keywords together in novel ways and have good synergy with other cards or archetypes. The cards you are known to generate elicit a 'wow, cool, that's a great idea!' response from players and leave them surprised by the card that generated. Remember, be extra creative!
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', 'rarity', 'explanation', and 'funnyExplanation' properties. The 'explanation' property should explain why the card was created the way it was. The 'funnyExplanation' property should be a hilarious explanation of why the card was created the way it was.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const string GenerateCardSystemPrompt = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules and obey the the Magic: The Gathering comprehensive rules.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', and 'rarity'.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const string GenerateCardSystemPromptWithExplanation = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules. The cards you generate should also obey the the Magic: The Gathering comprehensive rules as much as possible.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', 'rarity', 'explanation', and 'funnyExplanation' properties. The 'explanation' property should explain why the card was created the way it was. The 'funnyExplanation' property should be a hilarious explanation of why the card was created the way it was.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const float DefaultTemperature = 1;
        const float CreativeTemperature = 1.3F;

        public static int AllowedFreeGenerationsPerDay = 3;

        const int MaxPromptCharacters = 500;

        [Function("GenerateMagicCard")]
        [FunctionAuthorize]
        public static async Task<HttpResponseData> HttpStart([HttpTrigger(AuthorizationLevel.Anonymous, "GET")] HttpRequestData req, [DurableClient] DurableTaskClient client, FunctionContext context)
        {
            var log = context.GetLogger<GenerateMagicCardFunction>();
            
            // Extract request parameters.
            var rawUserPrompt = req.GetQueryParam("userPrompt") ?? string.Empty;
            rawUserPrompt = WebUtility.UrlDecode(rawUserPrompt);
            log.LogInformation($"User prompt: {rawUserPrompt?.Replace("\n", "")}");

            var model = req.GetQueryParam("model");
            var imageModel = req.GetQueryParam("imageModel") ?? Constants.Dalle2ModelName;
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
                        var errorMessage = $"You have exceeded your number of free generations per day ({AllowedFreeGenerationsPerDay}). Please enter your own OpenAI API key (https://platform.openai.com/api-keys) to continue generating. Sorry, this is a cost saving measure that I continue to scale back on :(";
                        return await req.CreateErrorResponse(HttpStatusCode.TooManyRequests, errorMessage);
                    }
                }
            }

            var request = new CardGenerationRequest
            {
                RawUserPrompt = rawUserPrompt,
                Model = model,
                ImageModel = imageModel,
                IncludeExplanation = includeExplanation,
                GenerateImagePrompt = generateImagePrompt,
                ExtraCreative = extraCreative,
                UserSuppliedApiKey = userSuppliedApiKey,
                Source = source,
                NumCards = numCards,
                UserName = userName,
                UserSubject = userSubject,
                UserLoggedIn = userLoggedIn,
                Host = req.Url.Host,
                Origin = req.Headers.TryGetValues("Origin", out var origins) ? origins.FirstOrDefault() : string.Empty
            };

            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync("GenerateMagicCardOrchestrator", request);
            log.LogInformation($"Started orchestration with ID '{instanceId}'.");

            var isLocal = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development";
            if (isLocal)
            {
                // The built in CreateCheckStatusResponseAsync method has the wrong port for URLs when running locally.
                // I do not know how to fix this, if it is a bug, or something I am doing wrong.
                var baseUrl = "http://localhost:7071/runtime/webhooks/durabletask/instances";
                var statusQueryGetUri = $"{baseUrl}/{instanceId}";

                var response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteAsJsonAsync(new
                {
                    id = instanceId,
                    statusQueryGetUri
                });
                return response;
            }
            else
            {
                // Use built-in method when hosted in Azure (it will get correct host/port)
                return await client.CreateCheckStatusResponseAsync(req, instanceId);
            }
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
                if (request.UserLoggedIn)
                {
                    await context.CallActivityAsync(nameof(UpdateUserRecord), new UpdateUserRecordInput() { Request = request, NumberOfCardsGenerated = cardResult.Cards.Length, TotalCost = cardResult.Cost.TotalCost });
                }

                // 6. Prepare final response with telemetry.
                output.Cards = cardResult.Cards.Select(x => new MagicCardResponse(x, x.Id, includeTemporaryImage: true));
                var json = JsonConvert.SerializeObject(output);

                // 7. Log telemetry
                var finalResponse = await context.CallActivityAsync<string>(nameof(LogTelemetry),
                    new FinalResponseData
                    {
                        Json = json,
                        CardResult = cardResult,
                        Request = request,
                        ImageOptions = imageOptions
                    });

                return json;
            }
            catch (Exception ex)
            {
                return FormatErrorMessage(ex, request.RawUserPrompt, request.UserSuppliedApiKey != null, request.UserLoggedIn);
            }
        }

        [Function("ValidateAndGetApiKey")]
        public static string ValidateAndGetApiKey([ActivityTrigger] CardGenerationRequest request, ILogger log)
        {
            var apiKeyToUse = request.UserSuppliedApiKey;

            if (string.IsNullOrWhiteSpace(apiKeyToUse))
            {
                if (request.UserLoggedIn)
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
            var systemPrompt = string.Empty;
            var cost = new Cost();
            var openAIResponse = string.Empty;
            var tokensUsed = 0;
            var attemptsToGenerateCard = 0;
            var actualGPTModelUsed = string.Empty;

           // Process raw user prompt.
            var rawUserPrompt = request.RawUserPrompt ?? string.Empty;
            if (rawUserPrompt.Length > MaxPromptCharacters)
            {
                log.LogWarning($"Truncating user prompt to {MaxPromptCharacters} characters.");
                rawUserPrompt = rawUserPrompt[..MaxPromptCharacters];
            }

            if (string.IsNullOrWhiteSpace(rawUserPrompt))
            {
                rawUserPrompt = "that is from the Dominaria plane.";
            }

            // Determine system prompt based on settings.
            var temperature = request.ExtraCreative ? CreativeTemperature : DefaultTemperature;
            if (request.ExtraCreative && request.IncludeExplanation)
            {
                systemPrompt = GenerateCreativeCardSystemPromptWithExplanation;
            }
            else if (request.ExtraCreative && !request.IncludeExplanation)
            {
                systemPrompt = GenerateCreativeCardSystemPrompt;
            }
            else if (!request.ExtraCreative && request.IncludeExplanation)
            {
                systemPrompt = GenerateCardSystemPromptWithExplanation;
            }
            else
            {
                systemPrompt = GenerateCardSystemPrompt;
            }

            // Format user prompt.
            var userPromptToSubmit = $"Please generate me one Magic: The Gathering card that has the following description: {rawUserPrompt}";
            if (request.NumCards > 1)
            {
                userPromptToSubmit = $"Please generate me {request.NumCards} different Magic: The Gathering cards based on the following description: {rawUserPrompt}";
            }

            // Select model and response format.
            var gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_3_5_Turbo;
            var chatResponseFormat = ChatCompletionCreateRequest.ResponseFormats.Text;
            if (!string.IsNullOrWhiteSpace(request.Model))
            {
                if (request.Model.StartsWith("gpt-4", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o_mini;
                    chatResponseFormat = ChatCompletionCreateRequest.ResponseFormats.Json;
                }
                else if (request.Model.StartsWith("gpt-3.5", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_3_5_Turbo;
                    chatResponseFormat = ChatCompletionCreateRequest.ResponseFormats.Text;
                }
            }

            log.LogInformation($"model: {request.Model}");
            log.LogInformation($"imageModel: {request.ImageModel}");
            log.LogInformation($"generateImagePrompt: {request.GenerateImagePrompt}");
            log.LogInformation($"includeExplanation: {request.IncludeExplanation}");
            log.LogInformation($"extraCreative: {request.ExtraCreative}");
            log.LogInformation($"generateImage: {request.ImageModel != "none"}");
            log.LogInformation($"numCards: {request.NumCards}");

            var openAIService = new OpenAIService(new OpenAIOptions()
            {
                ApiKey = apiKey
            });

            var openAICards = Array.Empty<BasicCard>();

            for (var attempt = 0; attempt < 5; attempt++)
            {
                attemptsToGenerateCard++;
                var stopwatch = Stopwatch.StartNew();

                var response = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = [ ChatMessage.FromUser(userPromptToSubmit), ChatMessage.FromSystem(systemPrompt) ],
                    Model = gptModel,
                    Temperature = temperature,
                    ChatResponseFormat = chatResponseFormat,
                });

                if (!response.Successful)
                {
                    throw new Exception(response.Error.Message);
                }

                actualGPTModelUsed = response.Model;
                tokensUsed += response.Usage.TotalTokens;
                cost.AddChatCost(response);

                openAIResponse = response.Choices[0].Message.Content;

                stopwatch.Stop();
                log?.LogMetric("CreateChatCompletionsAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                    properties: new Dictionary<string, object>()
                    {
                        { "attemptNumber", attemptsToGenerateCard },
                        { "estimatedCost", Pricing.GetCost(response) },
                        { "includeExplanation", request.IncludeExplanation.ToString() },
                        { "extraCreative", request.ExtraCreative },
                        { "model", actualGPTModelUsed },
                        { "systemPrompt", systemPrompt },
                        { "temperature", temperature },
                        { "tokensUsed", response.Usage.TotalTokens },
                        { "userSubject", request.UserSubject },
                        { "userPrompt", userPromptToSubmit },
                        { "userSuppliedKey", !string.IsNullOrWhiteSpace(request.UserSuppliedApiKey) }
                    });

                log?.LogInformation($"OpenAI response ({actualGPTModelUsed}):{Environment.NewLine}{openAIResponse}");

                try
                {
                    var deserialized = JsonConvert.DeserializeObject<OpenAIMagicCardResponse>(openAIResponse);
                    openAICards = deserialized.Cards;
                }
                catch (JsonReaderException)
                {
                    log?.LogWarning($"The initial response was not valid JSON.");
                    var captures = new Regex(@"(?<json>\{(.*)\})", RegexOptions.Singleline).GetNamedGroupsMatches(openAIResponse);
                    if (captures.ContainsKey("json"))
                    {
                        try
                        {
                            var deserialized = JsonConvert.DeserializeObject<OpenAIMagicCardResponse>(captures["json"]);
                            if (deserialized != null && deserialized.Cards != null)
                            {
                                openAICards = deserialized.Cards;
                            }
                        }
                        catch (JsonReaderException) { }
                    }
                }
                catch (JsonSerializationException)
                {
                    log?.LogWarning("Could not deserialize OpenAI response as OpenAIMagicCardResponse object. Now trying to deserialize as BasicCard[]...");
                    openAICards = JsonConvert.DeserializeObject<BasicCard[]>(openAIResponse);
                }

                if (openAICards.Length > 0)
                {
                    break;
                }
                else
                {
                    log?.LogError($"[Attempt {attempt + 1}] Unable to parse OpenAI response.");
                }
            }

            log?.LogInformation($"{tokensUsed} {actualGPTModelUsed} chat completion tokens used (${cost.TotalCost}).");

            // Parse basic cards into full Magic cards.
            var cards = openAICards
                .Select(x => MagicCardParser.Parse(x).Card)
                .ToArray()
                .Take(request.NumCards)
                .ToArray();

            foreach (var card in cards)
            {
                card.UserPrompt = rawUserPrompt;
                card.AlternativeImageIds = Array.Empty<string>();
                card.Id = Guid.NewGuid().ToString();
            }

            return new CardResult
            {
                Cards = cards,
                OpenAIResponse = openAIResponse,
                Temperature = temperature,
                TokensUsed = tokensUsed,
                ActualGPTModelUsed = actualGPTModelUsed,
                SystemPrompt = systemPrompt,
                Cost = cost,
                AttemptsToGenerateCard = attemptsToGenerateCard
            };
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
        public static async Task<ImageGenerationResult> GenerateCardImage([ActivityTrigger] ImageOptions imageOptions, FunctionContext functionContext) {
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
                            Temperature = cardResult.Temperature,
                            TokensUsed = cardResult.TokensUsed,
                            Model = cardResult.ActualGPTModelUsed,
                            ImageSize = request.ImageModel != "none" ? StaticValues.ImageStatics.Size.Size1024 : null,
                            ImageStyle = null,
                            ImageModel = request.ImageModel,
                            OpenAIResponse = cardResult.OpenAIResponse,
                            IncludeExplanation = request.IncludeExplanation,
                            ExtraCreative = request.ExtraCreative,
                            UserSupplied = !string.IsNullOrWhiteSpace(request.UserSuppliedApiKey),
                            EstimatedCost = cardResult.Cost.TotalCost,
                            Timestamp = DateTime.Now.ToUniversalTime(),
                            Host = request.Host,
                            Origin = request.Origin
                        },
                        User = new CardUserMetadata()
                        {
                            UserName = request.UserName,
                            UserSubject = request.UserSubject,
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
                var user = await usersCosmosClient.GetUserRecord(request.UserSubject);

                if (user == null)
                {
                    user = await usersCosmosClient.AddItemToContainerAsync(new User()
                    {
                        UserName = request.UserName,
                        UserSubject = request.UserSubject,
                    });
                }

                await usersCosmosClient.UserGeneratedCard(user, request.UserSubject, cardCount, totalCost, !string.IsNullOrWhiteSpace(request.UserSuppliedApiKey));
                log.LogInformation($"Updated user record for user '{request.UserSubject}' in database.");
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
                        { "numberOfChatCompletionAttempts", responseData.CardResult.AttemptsToGenerateCard },
                        { "systemPrompt", responseData.CardResult.SystemPrompt },
                        { "temperature", responseData.CardResult.Temperature },
                        { "userSubject", responseData.Request.UserSubject },
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
            var errorMessage = $"Error: {exception}";

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