using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Images;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    internal class OpenAIMagicCardResponse
    {
        public BasicCard[] Cards { get; set; }
    }

    internal class GenerateMagicCardFunctionResponse
    {
        [JsonProperty("cards")]
        public MagicCard[] Cards { get; set; }
    }

    public static class GenerateMagicCardFunction
    {
        const string GenerateCardSystemPrompt = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules and obey the the Magic: The Gathering comprehensive rules.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'text', 'flavorText', 'pt', and 'rarity'.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const string GenerateCardSystemPromptWithExplanation = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules. The cards you generate should also obey the the Magic: The Gathering comprehensive rules as much as possible.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'text', 'flavorText', 'pt', 'rarity', 'explanation', and 'funnyexplanation' properties. The 'explanation' property should explain why the card was created the way it was. The 'funnyexplanation' property should be a hilarious explanation of why the card was created the way it was.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const double temperature = 1;

        static readonly ImageSize imageSize = ImageSize._1024;

        [FunctionName("GenerateMagicCard")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            var rawUserPrompt = (string)req.Query["userPrompt"];
            log?.LogInformation($"User prompt: {rawUserPrompt.Replace("\n", "")}");
            var model = (string)req.Query["model"];
            var includeExplanation = bool.TryParse(req.Query["includeExplanation"], out bool result) && result;
            var apiKey = (string)req.Query["openAIApiKey"];
            var userSuppliedKey = true;
            var tokensUsed = 0;
            var estimatedCost = 0.0;
            var estimatedChatCompletionCost = 0.0;
            var estimatedImageGenerationCost = 0.0;
            var openAIResponse = "";

            var attemptsToGenerateCard = 0;
            var actualGPTModelUsed = "";

            var jwtToken = req.ReadJwtToken();
            var userName = Extensions.GetClaim(jwtToken, "name", defaultValue: "Anonymous");
            var userSubject = Extensions.GetClaim(jwtToken, "sub", defaultValue: "Anonymous");
            var userLoggedIn = !userSubject.Equals("Anonymous", StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                userSuppliedKey = false;

                if (userLoggedIn)
                {
                    apiKey = Environment.GetEnvironmentVariable(Constants.OpenAIApiKeyLoggedIn);
                }

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    apiKey = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);

                    if (string.IsNullOrWhiteSpace(apiKey))
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

            var gptModel = Model.GPT4;
            if (!string.IsNullOrWhiteSpace(model))
            {
                if (model.Equals("gpt-4", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = Model.GPT4;
                }
                else if (model.Equals("gpt-3.5", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = Model.ChatGPTTurbo;
                }
            }

            try
            {
                var api = new OpenAIAPI(new APIAuthentication(apiKey));
                var openAICards = Array.Empty<BasicCard>();

                for (var attempt = 0; attempt < 5; attempt++)
                {
                    attemptsToGenerateCard++;
                    var stopwatch = Stopwatch.StartNew();

                    var response = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
                    {
                        Messages = new ChatMessage[]
                        {
                            new ChatMessage(ChatMessageRole.User, userPromptToSubmit),
                            new ChatMessage(ChatMessageRole.System, systemPrompt)
                        },
                        Temperature = temperature,
                        Model = gptModel,
                    });

                    actualGPTModelUsed = response.Model.ModelID;
                    tokensUsed += response.Usage.TotalTokens;
                    var cost = Pricing.GetCost(response);
                    estimatedChatCompletionCost += cost;
                    estimatedCost += cost;

                    openAIResponse = response.Choices[0].Message.Content;

                    stopwatch.Stop();
                    log?.LogMetric("CreateChatCompletionsAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                        properties: new Dictionary<string, object>()
                        {
                            { "attemptNumber", attemptsToGenerateCard },
                            { "estimatedCost", estimatedChatCompletionCost },
                            { "includeExplanation", includeExplanation.ToString() },
                            { "model", actualGPTModelUsed },
                            { "requestId", response.RequestId },
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

                log?.LogInformation($"{tokensUsed} {actualGPTModelUsed} chat completion tokens used (${estimatedChatCompletionCost}).");

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

                // Generate an image for each card.
                foreach (var card in cards)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest()
                    {
                        NumOfImages = 1,
                        ResponseFormat = ImageResponseFormat.Url,
                        Size = imageSize,
                        Prompt = card.OpenAIImagePrompt,
                    });

                    var cost = Pricing.GetImageCost(imageSize);
                    estimatedImageGenerationCost += cost;
                    estimatedCost += cost;

                    card.ImageUrl = response.Data[0].Url;
                    log.LogInformation($"Card image url: {card.ImageUrl}");

                    stopwatch.Stop();
                    log.LogMetric("CreateImageAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                        properties: new Dictionary<string, object>()
                        {
                            { "imagePrompt", card.OpenAIImagePrompt },
                            { "imageUrl", card.ImageUrl },
                            { "imageSize", imageSize.ToString() },
                            { "requestId", response.RequestId },
                            { "userSubject", userSubject },
                        }); ;
                }

                var json = JsonConvert.SerializeObject(new GenerateMagicCardFunctionResponse() { Cards = cards });
                log?.LogInformation($"API JSON response:{Environment.NewLine}{JToken.Parse(json)}");

                log?.LogInformation($"Estimated cost: ${estimatedCost}");
                log?.LogMetric("GenerateMagicCard_EstimatedCost", estimatedCost, new Dictionary<string, object>()
                {
                    { "estimatedChatCompletionCost", estimatedChatCompletionCost },
                    { "estimatedCost", estimatedCost },
                    { "estimatedImageGenerationCost", estimatedImageGenerationCost },
                    { "imageSize", imageSize.ToString() },
                    { "includeExplanation", includeExplanation.ToString() },
                    { "model", actualGPTModelUsed },
                    { "numberOfChatCompletionAttempts", attemptsToGenerateCard },
                    { "systemPrompt", systemPrompt },
                    { "temperature", temperature },
                    { "userSubject", userSubject },
                    { "userPrompt", userPromptToSubmit },
                });

                try
                {
                    var blobStorageName = Extensions.GetSettingOrThrow(Constants.BlobStorageName);
                    var blobStorageEndpoint = Extensions.GetSettingOrThrow(Constants.BlobStorageEndpoint);
                    var blobStorageContainerName = Extensions.GetSettingOrThrow(Constants.BlobStorageContainerName);
                    var blobStorageAccessKey = Extensions.GetSettingOrThrow(Constants.BlobStorageAccessKey);
                    var cosmosDatabaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);

                    // For each generated card, store the card image in blob storage and insert a record into the database.
                    foreach (var card in cards)
                    {
                        var blobUrl = await Extensions.StoreImageInBlobAsync(card.ImageUrl, blobStorageName, blobStorageEndpoint, blobStorageContainerName, blobStorageAccessKey, log);
                        card.ImageUrl = blobUrl;

                        // Insert this record into the database.
                        var cardGenerationRecord = new CardGenerationRecord()
                        {
                            id = Guid.NewGuid().ToString(),
                            generationMetadata = new GenerationMetaData()
                            {
                                userPrompt = userPromptToSubmit,
                                systemPrompt = systemPrompt,
                                imagePrompt = cards.First().OpenAIImagePrompt,
                                temperature = temperature,
                                tokensUsed = tokensUsed,
                                model = actualGPTModelUsed,
                                imageSize = imageSize.ToString(),
                                openAIResponse = openAIResponse,
                                includeExplanation = includeExplanation,
                                userSupliedKey = userSuppliedKey,
                                estimatedCost = estimatedCost,
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
                    var cosmosDatabaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                    var usersCosmosClient = new CosmosClient(cosmosDatabaseId, Constants.CosmosDBUsersCollectionName, log);
                    if (!await usersCosmosClient.DocumentExists(id: userSubject))
                    {
                        await usersCosmosClient.AddItemToContainerAsync(new User()
                        {
                            userName = userName,
                            userSubject = userSubject,
                        });
                    }

                    await usersCosmosClient.UpdateUserRecord(userSubject, cards.Length, estimatedCost);
                    log.LogInformation("Updated user record for user '{userSubject}' in database.");
                }
                catch (Exception ex)
                {
                    // Non-fatal to overall operation.
                    log.LogError($"Failed to create or update user record: {ex}");
                }

                return new OkObjectResult(json);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Error: {exception}";

                if (exception.Source == "OpenAI_API" && 
                    exception.Message.ContainsIgnoreCase("Error at images/generations") && 
                    exception.Message.ContainsIgnoreCase("Your request was rejected as a result of our safety system"))
                {
                    errorMessage = $"Error: Your request to generate a card for prompt '{rawUserPrompt}' was rejected as a result of the AI language model safety system.";
                }

                if (exception.Source == "OpenAI_API" &&
                    exception.Message.ContainsIgnoreCase("That model is currently overloaded with other requests"))
                {
                    errorMessage = $"Error: Your request to generate a card for prompt '{rawUserPrompt}' failed because the AI language model is overloaded with requests. Please try again or use a different model.";
                }

                if (exception.Source == "OpenAI_API" &&
                    exception.Message.ContainsIgnoreCase("You exceeded your current quota"))
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

                if (exception.Source == "OpenAI_API" &&
                    exception.Message.ContainsIgnoreCase("OpenAI rejected your authorization"))
                {
                    if (userSuppliedKey)
                    {
                        errorMessage = $"Error: The OpenAI API key you provided is invalid. Please check the integrity of this API key.";
                        return new BadRequestObjectResult(errorMessage);
                    }
                    else if (!userSuppliedKey && userLoggedIn)
                    {
                        errorMessage = $"Error: The OpenAI API key provided by this website was invalid. Please supply your own Open AI API key in the settings to continue generating Magic: The Gathering cards!";
                        log?.LogError($"Invalid API key provided in website config: {apiKey.GetAsObfuscatedSecret(4)}");
                    }
                    else
                    {
                        errorMessage = $"Error: The OpenAI API key provided by this website was invalid. Please try logging in or supplying your own Open AI API key in the settings to continue generating Magic: The Gathering cards!";
                        log?.LogError($"Invalid API key provided in website config: {apiKey.GetAsObfuscatedSecret(4)}");
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
