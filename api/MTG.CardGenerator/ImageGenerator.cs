using Microsoft.Extensions.Logging;
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
        public static async Task<string> GenerateImage(string imagePrompt, string imageModel, bool detailedImagePrompt, string apiKey, ILogger log, Cost? cost = null)
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

            if (detailedImagePrompt)
            {
                imagePrompt = await GenerateDetailedImagePrompt(imagePrompt, apiKey, log, cost);
                log.LogInformation($"Detailed image prompt: {imagePrompt}");
            }

            var imageResult = await openAIService.Image.CreateImage(new ImageCreateRequest
            {
                Prompt = imagePrompt,
                N = 1,
                Size = StaticValues.ImageStatics.Size.Size1024,
                Model = imageModel,
                Quality = "standard",
                ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url
            });

            if (!imageResult.Successful)
            {
                throw new System.Exception($"Failed to generate image: {imageResult.Error.Message}");
            }

            return imageResult.Results.First().Url;
        }

        private static async Task<string> GenerateDetailedImagePrompt(string baseImagePrompt, string apiKey, ILogger log, Cost? cost = null)
        {
            var openAIService = new OpenAIService(new OpenAiOptions()
            {
                ApiKey = apiKey
            });

            var result = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages = new List<ChatMessage>
                {
                    ChatMessage.FromUser($"Please generate a detailed image prompt for the following: {baseImagePrompt}"),
                },
                // Model = OpenAI.ObjectModels.Models.Gpt_4_1106_preview,
                Model = OpenAI.ObjectModels.Models.Gpt_3_5_Turbo,
            });

            cost?.AddChatCost(result);

            return result.Choices.First().Message.Content;
        }
    }
}
