using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OpenAI_API.Images;
using System.Collections.Generic;

namespace MTG.CardGenerator
{
    // Represents a generated Magic: The Gathering card from OpenAI's LLM.
    public class OpenAIMagicCard
    {
        public string Name { get; set; }
        public string ManaCost { get; set; }
        public string Type { get; set; }
        [JsonProperty("text")]
        public string OracleText { get; set; }
        public string FlavorText { get; set; }
        public string Rarity { get; set; }
        [JsonProperty("pt")]
        public string PowerAndToughness { get; set; }
        // Sometimes OpenAI returns power separately, despite being told to return "pt" as one field.
        public string Power { get; set; }
        // Sometimes OpenAI returns toughness separately, despite being told to return "pt" as one field.
        public string Toughness { get; set; }
    }

    public class OpenAIMagicCardResponse
    {
        public OpenAIMagicCard[] Cards { get; set; }
    }

    public class GenerateMagicCardFunctionResponse
    {
        [JsonProperty("cards")]
        public MagicCard[] Cards { get; set; }
    }

    public static class GenerateMagicCardFunction
    {
        const string GenerateCardSystemPrompt = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules. The cards you generate should also obey the the Magic: The Gathering comprehensive rules as much as possible.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'text', 'flavorText', 'pt', and 'rarity' properties.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        [FunctionName("GenerateMagicCard")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var userPrompt = (string) req.Query["userPrompt"];
                log?.LogInformation($"User prompt: {userPrompt.Replace("\n", "")}");

                if (string.IsNullOrWhiteSpace(userPrompt))
                {
                    userPrompt = "that is from the Dominaria plane.";
                }

                var userPromptToSubmit = $"Generate me one Magic: The Gathering card that {userPrompt}";

                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                OpenAIAPI api = new OpenAIAPI(new APIAuthentication(apiKey));
                var openAICards = Array.Empty<OpenAIMagicCard>();

                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var response = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
                    {
                        Messages = new ChatMessage[]
                        {
                            new ChatMessage(ChatMessageRole.User, userPromptToSubmit),
                            new ChatMessage(ChatMessageRole.System, GenerateCardSystemPrompt)
                        },
                        Temperature = 1,
                        Model = Model.ChatGPTTurbo,
                    });

                    var reply = response.Choices[0].Message.Content;

                    stopwatch.Stop();
                    log?.LogMetric("CreateChatCompletionsAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                        properties: new Dictionary<string, object>()
                        {
                            { "response", response },
                            { "systemPrompt", GenerateCardSystemPrompt },
                            { "userPrompt", userPromptToSubmit },
                            { "temperature", 1 },
                            { "model", Model.ChatGPTTurbo },
                            { "requestId", response.RequestId }
                        });

                    log?.LogInformation($"OpenAI response:{Environment.NewLine}{reply}");

                    try
                    {
                        var deserialized = JsonConvert.DeserializeObject<OpenAIMagicCardResponse>(reply);
                        openAICards = deserialized.Cards;
                    }
                    catch (JsonReaderException)
                    {
                        // Sometimes the response does not obey the prompt and includes text at the beginning or end of the JSON.
                        // Try to extract JSON from regex instead.
                        log?.LogWarning($"The initial response was not valid JSON.");
                        var captures = new Regex(@"(?<json>\{(.*)\})", RegexOptions.Singleline).GetNamedGroupsMatches(reply);
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

                    if (openAICards.Length > 0)
                    {
                        break;
                    }
                    else
                    {
                        log?.LogError($"[Attempt {attempt+1}] Unable to parse OpenAI response.");
                    }
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
                        Size = ImageSize._1024,
                        Prompt = card.OpenAIImagePrompt,
                    });

                    card.ImageUrl = response.Data[0].Url;
                    log.LogInformation($"Card image url: {card.ImageUrl}");

                    stopwatch.Stop();
                    log.LogMetric("CreateImageAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                        properties: new Dictionary<string, object>()
                        {
                            { "imagePrompt", card.OpenAIImagePrompt },
                            { "imageUrl", card.ImageUrl },
                            { "requestId", response.RequestId }
                        });
                }

                var json = JsonConvert.SerializeObject(new GenerateMagicCardFunctionResponse() { Cards = cards });
                log?.LogInformation($"API JSON response:{Environment.NewLine}{JToken.Parse(json)}");
                return new OkObjectResult(json);
            }
            catch (Exception exception)
            {
                var errorMessage = $"Unexpected exception: {exception}";
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
