using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static OpenAI.ObjectModels.StaticValues.ImageStatics;

namespace MTG.CardGenerator
{
    public static class GenerateMagicCardFunction
    {
        private class OpenAIMagicCardResponse
        {
            public BasicCard[] Cards { get; set; }
        }

        private class GenerateMagicCardFunctionResponse
        {
            [JsonProperty("cards")]
            public MagicCard[] Cards { get; set; }
        }

        const string GenerateCardSystemPrompt = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules and obey the the Magic: The Gathering comprehensive rules.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'text', 'flavorText', 'pt', and 'rarity'.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const string GenerateCardSystemPromptWithExplanation = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules. The cards you generate should also obey the the Magic: The Gathering comprehensive rules as much as possible.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'text', 'flavorText', 'pt', 'rarity', 'explanation', and 'funnyExplanation' properties. The 'explanation' property should explain why the card was created the way it was. The 'funnyExplanation' property should be a hilarious explanation of why the card was created the way it was.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const float temperature = 1;

        const int AllowedFreeGenerationsPerDay = 25;

        static readonly string imageSize = Size.Size1024;

        [FunctionName("GenerateMagicCard")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            var rawUserPrompt = (string)req.Query["userPrompt"];
            log?.LogInformation($"User prompt: {rawUserPrompt.Replace("\n", "")}");
            var model = (string)req.Query["model"];
            var includeExplanation = bool.TryParse(req.Query["includeExplanation"], out bool result) && result;
            var highQualityImage = bool.TryParse(req.Query["highQualityImage"], out bool hd) && hd;
            var userSuppliedApiKey = (string)req.Query["openAIApiKey"];
            var cosmosDatabaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
            var userSuppliedKey = true;
            var tokensUsed = 0;
            var cost = new Cost();
            var openAIResponse = "";

            var attemptsToGenerateCard = 0;
            var actualGPTModelUsed = "";

            var jwtToken = req.ReadJwtToken();
            var userName = Extensions.GetClaim(jwtToken, "name", defaultValue: "Anonymous");
            var userSubject = Extensions.GetClaim(jwtToken, "sub", defaultValue: "Anonymous");
            var userLoggedIn = !userSubject.Equals("Anonymous", StringComparison.OrdinalIgnoreCase);
            var usersCosmosClient = new CosmosClient(cosmosDatabaseId, Constants.CosmosDBUsersCollectionName, log);
            var user = await usersCosmosClient.GetUserRecord(userSubject);

            // If the user is logged in and has not provided an API key, limit the number of free generations they can do per day.
            if (userLoggedIn && string.IsNullOrWhiteSpace(userSuppliedApiKey))
            {
                if (user != null && user.id != "fd42ec51-676d-479a-bab4-b7e7b86887e8")
                {
                    if (user.lastActiveTime?.Date == DateTime.Now.ToUniversalTime().Date && user.numberOfFreeCardsGeneratedToday >= AllowedFreeGenerationsPerDay)
                    {
                        return new ContentResult
                        {
                            StatusCode = 429,
                            Content = $"You have exceeded your number of free generations for the day ({AllowedFreeGenerationsPerDay}). Try again tomorrow or enter your own Open AI API key in the settings to continue generating!",
                        };
                    }
                }
            }

            var apiKeyToUse = userSuppliedApiKey;
            if (string.IsNullOrWhiteSpace(apiKeyToUse))
            {
                userSuppliedKey = false;

                if (userLoggedIn)
                {
                    apiKeyToUse = Environment.GetEnvironmentVariable(Constants.OpenAIApiKeyLoggedIn);
                }

                if (string.IsNullOrWhiteSpace(apiKeyToUse))
                {
                    apiKeyToUse = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);

                    if (string.IsNullOrWhiteSpace(apiKeyToUse))
                    {
                        return new BadRequestObjectResult("No valid OpenAI API key was provided. Please set your OpenAI API key in the settings and try again.");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(rawUserPrompt))
            {
                rawUserPrompt = "that is from the Dominaria plane.";
            }

            var systemPrompt = includeExplanation ? GenerateCardSystemPromptWithExplanation : GenerateCardSystemPrompt;
            var userPromptToSubmit = $"Please generate me one 'Magic: The Gathering card' that has the following description: {rawUserPrompt}";

            var gptModel = OpenAI.ObjectModels.Models.Gpt_4;
            var chatResponseFormat = ChatCompletionCreateRequest.ResponseFormats.Json;
            if (!string.IsNullOrWhiteSpace(model))
            {
                if (model.Equals("gpt-4", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = OpenAI.ObjectModels.Models.Gpt_4;
                }
                else if (model.Equals("gpt-4-1106-preview", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = OpenAI.ObjectModels.Models.Gpt_4_1106_preview;
                }
                else if (model.Equals("gpt-3.5", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo;
                    // JSON not supported in gpt-3.5.
                    chatResponseFormat = ChatCompletionCreateRequest.ResponseFormats.Text;
                }
            }

            try
            {
                var openAIService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = apiKeyToUse
                });

                var openAICards = Array.Empty<BasicCard>();

                for (var attempt = 0; attempt < 5; attempt++)
                {
                    attemptsToGenerateCard++;
                    var stopwatch = Stopwatch.StartNew();

                    var response = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                    {
                        Messages = new List<ChatMessage>
                        {
                            ChatMessage.FromUser(userPromptToSubmit),
                            ChatMessage.FromSystem(systemPrompt),
                        },
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
                            { "includeExplanation", includeExplanation.ToString() },
                            { "model", actualGPTModelUsed },
                            { "response", response },
                            { "systemPrompt", systemPrompt },
                            { "temperature", temperature },
                            { "tokensUsed", response.Usage.TotalTokens },
                            { "userSubject", userSubject },
                            { "userPrompt", userPromptToSubmit },
                            { "userSuppliedKey", userSuppliedKey },
                        });

                    log?.LogInformation($"OpenAI response ({actualGPTModelUsed}):{Environment.NewLine}{openAIResponse}");

                    try
                    {
                        var deserialized = JsonConvert.DeserializeObject<OpenAIMagicCardResponse>(openAIResponse);
                        openAICards = deserialized.Cards;
                    }
                    catch (JsonReaderException)
                    {
                        // Sometimes the response does not obey the prompt and includes text at the beginning or end of the JSON.
                        // Try to extract JSON from regex instead.
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
                        log?.LogError($"[Attempt {attempt+1}] Unable to parse OpenAI response.");
                    }
                }

                log?.LogInformation($"{tokensUsed} {actualGPTModelUsed} chat completion tokens used (${cost.TotalCost}).");

                if (openAICards.Length == 0)
                {
                    return new ContentResult
                    {
                        StatusCode = 500,
                        Content = $"Error: Unable to generate a card for the prompt '{rawUserPrompt}'. Your request may have been rejected as a result of the AI language model safety system.",
                    };
                }

                // Attach the user prompt to each card.
                foreach (var card in openAICards)
                {
                    card.UserPrompt = rawUserPrompt;
                }

                // Parse the cards. If multiple were generated, only process and image for and return one the first one.
                var cards = openAICards.Select(x => new MagicCard(x)).ToArray().Take(1).ToArray();

                var imageModel = Constants.Dalle2ModelName;
                if (new Random().Next(1, 4) == 1)
                {
                    // 1/3 of the time use Dalle3.
                    imageModel = Constants.Dalle3ModelName;
                }

                if (highQualityImage)
                {
                    imageModel = Constants.Dalle3ModelName;
                }

                // Generate an image for each card.
                foreach (var card in cards)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var imagePrompt = ImageGenerator.GetImagePromptForCard(card, imageModel);

                    if (highQualityImage)
                    {
                        imagePrompt = await ImageGenerator.GenerateDetailedImagePrompt(card, imagePrompt, apiKeyToUse, log, cost);
                    }

                    var url = await ImageGenerator.GenerateImage(imagePrompt, imageModel, apiKeyToUse, log, cost);
                    card.TemporaryImageUrl = url;
                    stopwatch.Stop();

                    log.LogMetric("CreateImageAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                        properties: new Dictionary<string, object>()
                        {
                            { "imagePrompt", imagePrompt },
                            { "imageModel", imageModel },
                            { "imageUrl", card.ImageUrl },
                            { "imageSize", imageSize },
                            { "userSubject", userSubject },
                        });
                }

                try
                {
                    var blobStorageName = Extensions.GetSettingOrThrow(Constants.BlobStorageName);
                    var blobStorageEndpoint = Extensions.GetSettingOrThrow(Constants.BlobStorageEndpoint);
                    var blobStorageContainerName = Extensions.GetSettingOrThrow(Constants.BlobStorageContainerName);
                    var blobStorageAccessKey = Extensions.GetSettingOrThrow(Constants.BlobStorageAccessKey);

                    // For each generated card, store the card image in blob storage and insert a record into the database.
                    foreach (var card in cards)
                    {
                        var blobUrl = await Extensions.StoreImageInBlobAsync(card.TemporaryImageUrl, blobStorageName, blobStorageEndpoint, blobStorageContainerName, blobStorageAccessKey, log: log);
                        card.ImageUrl = blobUrl;

                        // Insert this record into the database.
                        var cardGenerationRecord = new CardGenerationRecord()
                        {
                            id = Guid.NewGuid().ToString(),
                            generationMetadata = new GenerationMetaData()
                            {
                                userPrompt = userPromptToSubmit,
                                systemPrompt = systemPrompt,
                                imagePrompt = ImageGenerator.GetImagePromptForCard(cards.First(), imageModel),
                                temperature = temperature,
                                tokensUsed = tokensUsed,
                                model = actualGPTModelUsed,
                                imageSize = imageSize,
                                imageModel = imageModel,
                                openAIResponse = openAIResponse,
                                includeExplanation = includeExplanation,
                                userSupliedKey = userSuppliedKey,
                                estimatedCost = cost.TotalCost,
                                timestamp = DateTime.Now.ToUniversalTime(),
                            },
                            user = new UserMeta()
                            {
                                userName = userName,
                                userSubject = userSubject,
                            },
                            magicCards = cards,
                        };

                        var cardsCosmosClient = new CosmosClient(cosmosDatabaseId, Constants.CosmosDBCardsCollectionName, log);
                        await cardsCosmosClient.AddItemToContainerAsync(cardGenerationRecord);
                        log.LogInformation($"Wrote card generation record to database '{cosmosDatabaseId}'.");
                    }

                }
                catch (Exception ex)
                {
                    // Non-fatal to overall operation.
                    log.LogError($"Failed to store generated card: {ex}");
                }

                try
                {
                    if (userLoggedIn)
                    {
                        if (user == null)
                        {
                            user = await usersCosmosClient.AddItemToContainerAsync(new User()
                            {
                                userName = userName,
                                userSubject = userSubject,
                            });
                        }

                        await usersCosmosClient.UpdateUserRecord(user, userSubject, cards.Length, cost.TotalCost, !string.IsNullOrWhiteSpace(userSuppliedApiKey));
                        log.LogInformation($"Updated user record for user '{userSubject}' in database.");
                    }
                }
                catch (Exception ex)
                {
                    // Non-fatal to overall operation.
                    log.LogError($"Failed to create or update user record: {ex}");
                }

                var json = JsonConvert.SerializeObject(new GenerateMagicCardFunctionResponse() { Cards = cards });
                log?.LogInformation($"API JSON response:{Environment.NewLine}{JToken.Parse(json)}");

                log?.LogInformation($"Estimated cost: ${cost.TotalCost}");
                log?.LogMetric("GenerateMagicCard_EstimatedCost", cost.TotalCost, new Dictionary<string, object>()
                {
                    { "estimatedCost", cost.TotalCost },
                    { "imageSize", imageSize },
                    { "imageModel", imageModel },
                    { "includeExplanation", includeExplanation.ToString() },
                    { "highQualityImage", highQualityImage.ToString() },
                    { "model", actualGPTModelUsed },
                    { "numberOfChatCompletionAttempts", attemptsToGenerateCard },
                    { "systemPrompt", systemPrompt },
                    { "temperature", temperature },
                    { "userSubject", userSubject },
                    { "userPrompt", userPromptToSubmit },
                });

                return new OkObjectResult(json);
            }
            catch (Exception exception)
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
                        return new BadRequestObjectResult(errorMessage);
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
                        return new BadRequestObjectResult(errorMessage);
                    }
                    else if (!userSuppliedKey && userLoggedIn)
                    {
                        errorMessage = $"Error: The OpenAI API key provided by this website was invalid. Please supply your own Open AI API key in the settings to continue generating Magic: The Gathering cards!";
                        log?.LogError($"Invalid API key provided in website config: {apiKeyToUse.GetAsObfuscatedSecret(4)}");
                    }
                    else
                    {
                        errorMessage = $"Error: Please log in or supply your own Open AI API key in the settings to generate Magic: The Gathering cards!";
                        log?.LogError($"Invalid API key provided in website config: {apiKeyToUse.GetAsObfuscatedSecret(4)}");
                    }
                }

                log?.LogError(exception, errorMessage);
                return new ContentResult
                {
                    StatusCode = 500,
                    Content = errorMessage,
                };
            }
        }
    }
}
