using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class ImageGenerator
    {
        public static string GetImagePromptForCard(MagicCard card, string imageModel)
        {
            var prompt = $"{card.Name}: {card.FlavorText}";

            if (card.Type == CardType.Creature)
            {
                if (imageModel == Constants.Dalle2ModelName)
                {
                    prompt = $"'An image of {card.Name}, a {card.TypeLine}: {card.FlavorText}. Greg Kutkowski style, digital, fantasy art.";
                }

                if (imageModel == Constants.Dalle3ModelName)
                {
                    // Dalle 3 does not like the flavor text in the prompt usually. It leads to a lot of text in the images.
                    prompt = $"An image of {card.Name}, a {card.TypeLine}. Greg Kutkowski style, digital, fantasy art.";
                }
            }

            if (card.Type == CardType.Instant || card.Type == CardType.Sorcery || card.Type == CardType.Enchantment || card.Type == CardType.Artifact)
            {
                prompt = $"An image of {card.Name}: {card.FlavorText}. Greg Kutkowski style, digital, fantasy art.";
            }

            if (card.Type == CardType.Enchantment || card.Type == CardType.Artifact)
            {
                prompt = $"An image of {card.Name}: {card.FlavorText}. Greg Kutkowski style, digital, fantasy art.";
            }

            return prompt;
        }

        public static async Task<string> GenerateImage(string imagePrompt, string imageModel, string apiKey, ILogger log, Cost? cost = null)
        {
            if (imageModel != Constants.Dalle2ModelName && imageModel != Constants.Dalle3ModelName)
            {
                throw new System.Exception($"Invalid image model: {imageModel}. Expecting '{Constants.Dalle2ModelName}' or '{Constants.Dalle3ModelName}'.");
            }

            var openAIService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            log.LogInformation($"{imageModel} image prompt: {imagePrompt}");

            var imageResult = await openAIService.Image.CreateImage(new ImageCreateRequest
            {
                Prompt = imagePrompt,
                N = 1,
                Size = StaticValues.ImageStatics.Size.Size1024,
                Model = imageModel,
                Quality = "standard",
                ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url
            });

            cost?.AddImageCost(StaticValues.ImageStatics.Size.Size1024, imageModel);

            if (!imageResult.Successful)
            {
                throw new System.Exception($"Failed to generate image: {imageResult.Error.Message}");
            }

            return imageResult.Results.First().Url;
        }

        public static async Task<string> GenerateDetailedImagePrompt(MagicCard card, string baseImagePrompt, string apiKey, ILogger log, Cost? cost = null)
        {
            var openAIService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            var prompt = $"Please generate a detailed image prompt for the following: {baseImagePrompt}";

            /*
            var prompt2 = @$"Please generate a detailed prompt for this Magic: The Gathering card:
{JsonConvert.SerializeObject(card)}.
Greg Kutkowski style, digital, fantasy art. Output the image prompt and nothing else.";
            */

            var result = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromUser(prompt),
                },
                // Model = OpenAI.ObjectModels.Models.Gpt_4_1106_preview,
                Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo,
            });

            cost?.AddChatCost(result);

            return result.Choices.First().Message.Content;
        }
    }
}
