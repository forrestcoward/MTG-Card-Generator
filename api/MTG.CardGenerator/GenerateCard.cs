using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using static MTG.CardGenerator.GenerateMagicCardFunction;

namespace MTG.CardGenerator
{
    public class CardGenerationRequest
    {
        public string RawUserPrompt { get; set; }
        public string Model { get; set; }
        public string ImageModel { get; set; }
        public bool IncludeExplanation { get; set; }
        public bool GenerateImagePrompt { get; set; }
        public bool ExtraCreative { get; set; }
        public string Source { get; set; }
        public int NumCards { get; set; }
        public RequestMetadata Metadata { get; set; }
    }

    public class CardResult
    {
        public MagicCard[] Cards { get; set; }
        public string OpenAIResponse { get; set; }
        public int TokensUsed { get; set; }
        public string ActualGPTModelUsed { get; set; }
        public string SystemPrompt { get; set; }
        public Cost Cost { get; set; }
        public int AttemptsToGenerateCard { get; set; }
    }

    public class OpenAIMagicCardResponse
    {
        public BasicCard[] Cards { get; set; }
    }

    internal static class GenerateCard
    {
        private const string GenerateCreativeCardSystemPrompt = $@"
You are an assistant who works as a Magic: The Gathering card designer. The cards you generate should be as creative as possible and utilize Magic: The Gathering card mechanics in unique and interesting ways, ideally mixing multiple mechanics and keywords together in novel ways and have good synergy with other cards or archetypes. The cards you are known to generate elicit a 'wow, cool, that's a great idea!' response from players and leave them surprised by the card that generated. Remember, be extra creative!
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', and 'rarity'.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        private const string GenerateCreativeCardSystemPromptWithExplanation = $@"
You are an assistant who works as a Magic: The Gathering card designer. The cards you generate should be as creative as possible and utilize Magic: The Gathering card mechanics in unique and interesting ways, ideally mixing multiple mechanics and keywords together in novel ways and have good synergy with other cards or archetypes. The cards you are known to generate elicit a 'wow, cool, that's a great idea!' response from players and leave them surprised by the card that generated. Remember, be extra creative!
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', 'rarity', 'explanation', and 'funnyExplanation' properties. The 'explanation' property should explain why the card was created the way it was. The 'funnyExplanation' property should be a hilarious explanation of why the card was created the way it was.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        private const string GenerateCardSystemPrompt = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules and obey the the Magic: The Gathering comprehensive rules.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', and 'rarity'.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        private const string GenerateCardSystemPromptWithExplanation = $@"
You are an assistant who works as a Magic: The Gathering card designer. You like complex cards with interesting mechanics. The cards you generate should obey the Magic 'color pie' design rules. The cards you generate should also obey the the Magic: The Gathering comprehensive rules as much as possible.
You should return a JSON array named 'cards' where each entry represents a card you generated for the user based on their request. Each card must include the 'name', 'manaCost', 'type', 'oracleText', 'flavorText', 'pt', 'rarity', 'explanation', and 'funnyExplanation' properties. The 'explanation' property should explain why the card was created the way it was. The 'funnyExplanation' property should be a hilarious explanation of why the card was created the way it was.
Do not explain the cards or explain your reasoning. Only return the JSON of cards named 'cards'.";

        private const int MaxPromptCharacters = 500;

        public static async Task<CardResult> Generate(CardGenerationRequest request, string apiKey, Microsoft.Extensions.Logging.ILogger log)
        {
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
            var gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o;
            var chatResponseFormat = ChatCompletionCreateRequest.ResponseFormats.Json;
            if (!string.IsNullOrWhiteSpace(request.Model))
            {
                if (request.Model.Equals("gpt-4o-mini", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o_mini;
                }
                else if (request.Model.Equals("gpt-4o", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o;
                }
                else if (request.Model.StartsWith("gpt-o3", StringComparison.OrdinalIgnoreCase))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.O3_mini;
                }
                else if (request.Model.StartsWith("gpt-41"))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4_1;
                }
                else if (request.Model.StartsWith("gpt-41-mini"))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4_1_mini;
                }
                else if (request.Model.StartsWith("gt-41-nano"))
                {
                    gptModel = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4_1_nano;
                }
                else if (request.Model.StartsWith("gpt-35", StringComparison.OrdinalIgnoreCase))
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

            var openAIService = new OpenAIService(new OpenAIOptions() { ApiKey = apiKey });
            var openAICards = Array.Empty<BasicCard>();

            for (var attempt = 0; attempt < 5; attempt++)
            {
                attemptsToGenerateCard++;

                var response = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = [ChatMessage.FromUser(userPromptToSubmit), ChatMessage.FromSystem(systemPrompt)],
                    Model = gptModel,
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
                log?.LogInformation($"OpenAI response ({actualGPTModelUsed}):{Environment.NewLine}{openAIResponse}");

                if (request.ExtraCreative)
                {
                    var mechanicsToUsePrompt = CardMechanicSelector.GetRandomMechanicsAsPrompt(evergreenCount: 0, keywordActionCount: 1, mechanicCount: 1);

                    log.LogInformation($"Mechanics to use:{Environment.NewLine}: {mechanicsToUsePrompt}");

                    var userPrompt = $"The card you are updating is the following: {openAIResponse} ";

                    var extraCreativeSystemPrompt = $@"
                        You are an assistant who works as a Magic: The Gathering card designer. Your job is to update an existing card's oracle text to incorporate new mechanics into the card to make it more interesting. You will be given an existing card and a set of new mechanics to incorporate into the card. Think step by step when incorporating the new mechanics into the card: Please ensure that the new mechanics are incorporated in a consistent and coherent way with regards to the rest of the card. 

                        The mechanics you should incorporate into the card are the following: {mechanicsToUsePrompt}

                        You should return the card JSON with the updated 'oracleText' field.
                        Do not explain the cards or explain your reasoning. Only return the JSON with the updated `oracleText` that incorporates the new mechanics.";

                    var updatedCardReponse = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                    {
                        Messages = [ChatMessage.FromUser(userPrompt), ChatMessage.FromSystem(extraCreativeSystemPrompt)],
                        Model = gptModel,
                        ChatResponseFormat = chatResponseFormat,
                    });

                    openAIResponse = updatedCardReponse.Choices[0].Message.Content;
                    tokensUsed += response.Usage.TotalTokens;
                    cost.AddChatCost(updatedCardReponse);
                    log?.LogInformation($"[Extra creative] OpenAI response to update the card: ({actualGPTModelUsed}):{Environment.NewLine}{openAIResponse}");
                }

                var checkConsistency = true;
                if (checkConsistency)
                {
                    var userPrompt = $"The card you are updating is the following: {openAIResponse} ";

                    var checkConsistencySystemPrompt = $@"
                        You are a Magic: The Gathering card designer assistant. Your task is to:

                        1. **Validate** an existing card’s `oracleText` for rule consistency. 
                           - E.g., creatures without flash shouldn’t be able to counter spells (they can’t be cast at instant speed).  
                           - Artifacts can’t have haste (they’re not affected by summoning sickness).

                        2. **If inconsistent**, rewrite **only** the `oracleText` so the ability makes sense within MTG’s rules.

                        3. **Output**: Return the full card JSON, updating only the `""oracleText""` field.  
                           - **Do not** include explanations, reasoning, or any other modifications.

                        **Think step-by-step** through relevant rules as you validate and rewrite.";

                    var updatedCardReponse = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                    {
                        Messages = [ChatMessage.FromUser(userPrompt), ChatMessage.FromSystem(checkConsistencySystemPrompt)],
                        Model = gptModel,
                        ChatResponseFormat = chatResponseFormat,
                    });

                    openAIResponse = updatedCardReponse.Choices[0].Message.Content;
                    tokensUsed += response.Usage.TotalTokens;
                    cost.AddChatCost(updatedCardReponse);
                    log?.LogInformation($"[Check consistency] OpenAI response to update the card: ({actualGPTModelUsed}):{Environment.NewLine}{openAIResponse}");
                }

                try
                {
                    var deserialized = JsonConvert.DeserializeObject<OpenAIMagicCardResponse>(openAIResponse);
                    openAICards = deserialized.Cards;
                }
                catch (JsonReaderException)
                {
                    log?.LogWarning($"The initial response was not valid JSON.");
                    var captures = new Regex(@"(?<json>\{(.*)\})", RegexOptions.Singleline).GetNamedGroupsMatches(openAIResponse);
                    if (captures.TryGetValue("json", out string value))
                    {
                        try
                        {
                            var deserialized = JsonConvert.DeserializeObject<OpenAIMagicCardResponse>(value);
                            if (deserialized != null && deserialized.Cards != null)
                            {
                                openAICards = deserialized.Cards;
                            }
                        }
                        catch (JsonReaderException)
                        {
                        }
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

            // Parse basic cards into full Magic cards.
            var cards = openAICards
                .Select(x => MagicCardParser.Parse(x).Card)
                .ToArray()
                .Take(request.NumCards)
                .ToArray();

            foreach (var card in cards)
            {
                card.UserPrompt = rawUserPrompt;
                card.AlternativeImageIds = [];
                card.Id = Guid.NewGuid().ToString();
            }

            var cardResult = new CardResult
            {
                Cards = cards,
                OpenAIResponse = openAIResponse,
                TokensUsed = tokensUsed,
                ActualGPTModelUsed = actualGPTModelUsed,
                SystemPrompt = systemPrompt,
                Cost = cost,
                AttemptsToGenerateCard = attemptsToGenerateCard
            };
            return cardResult;
        }
    }
}
