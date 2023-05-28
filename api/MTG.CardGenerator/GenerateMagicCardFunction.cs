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
    public class BasicCard
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
        // Reason why the LLM genearted the card.
        public string Explanation { get; set; }
        // A funny explanation of why the LLM generated the card.
        public string FunnyExplanation { get; set; }
        // The user prompt that generated this card.
        public string UserPrompt { get; set; }
    }

    public class OpenAIMagicCardResponse
    {
        public BasicCard[] Cards { get; set; }
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
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'text', 'flavorText', 'pt', and 'rarity'.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        const string GenerateCardSystemPromptWithExplaination = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules. The cards you generate should also obey the the Magic: The Gathering comprehensive rules as much as possible.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'text', 'flavorText', 'pt', 'rarity', 'explanation', and 'funnyExplanation' properties. The 'explanation' property should explain why the card was created the way it was. The 'funnyExplanation' property should be a hilarious explanation of why the card was created the way it was.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        [FunctionName("GenerateMagicCard")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            var rawUserPrompt = (string)req.Query["userPrompt"];
            log?.LogInformation($"User prompt: {rawUserPrompt.Replace("\n", "")}");
            var model = (string)req.Query["model"];
            var includeExplaination = bool.TryParse(req.Query["includeExplaination"], out bool result) && result;
            var apiKey = (string)req.Query["openAIApiKey"];
            var userSuppliedKey = true;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                userSuppliedKey = false;
                
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new BadRequestObjectResult("No valid OpenAI API key was provided. Please set your OpenAI API key in the settings and try again.");
                }
            }

            if (string.IsNullOrWhiteSpace(rawUserPrompt))
            {
                rawUserPrompt = "that is from the Dominaria plane.";
            }

            var systemPrompt = includeExplaination ? GenerateCardSystemPromptWithExplaination : GenerateCardSystemPrompt;
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
                    var stopwatch = Stopwatch.StartNew();
                    var response = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
                    {
                        Messages = new ChatMessage[]
                        {
                            new ChatMessage(ChatMessageRole.User, userPromptToSubmit),
                            new ChatMessage(ChatMessageRole.System, systemPrompt)
                        },
                        Temperature = 1,
                        Model = gptModel,
                    });

                    var reply = response.Choices[0].Message.Content;

                    stopwatch.Stop();
                    log?.LogMetric("CreateChatCompletionsAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                        properties: new Dictionary<string, object>()
                        {
                            { "response", response },
                            { "systemPrompt", systemPrompt },
                            { "userPrompt", userPromptToSubmit },
                            { "temperature", 1 },
                            { "model", gptModel.ModelID },
                            { "includeExplaination", includeExplaination.ToString() },
                            { "requestId", response.RequestId }
                        });

                    log?.LogInformation($"OpenAI response ({gptModel.ModelID}):{Environment.NewLine}{reply}");

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
                    catch (JsonSerializationException)
                    {
                        log?.LogWarning("Could not deserialize OpenAI response as OpenAIMagicCardResponse object. Now trying to deserialize as BasicCard[]...");
                        openAICards = JsonConvert.DeserializeObject<BasicCard[]>(reply);
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
                    else
                    {
                        errorMessage = $"Error: The OpenAI API key used by this website has exceeded its quota due to some users spamming requests :( Please set your own OpenAI API key in the settings to continue generating Magic: The Gathering cards.";
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
                    else
                    {
                        errorMessage = $"Error: The OpenAI API key used by this website is invalid and must be fixed by the website owners.";
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
