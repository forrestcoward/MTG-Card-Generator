using Betalgo.Ranul.OpenAI;
using Betalgo.Ranul.OpenAI.Managers;
using Betalgo.Ranul.OpenAI.ObjectModels;
using Betalgo.Ranul.OpenAI.ObjectModels.RequestModels;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;

namespace MTG.CardGenerator
{
    public static class ImageGenerator
    {
        public class ImageGenerationOptions
        {
            public string Prompt { get; set; }
            public string Style { get; set; }
            public string Model { get; set; }
            public string Size { get; set; }
        }

        // Choose randomly for each card.
        private static readonly string[] Artists =
        [
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Terese Nielsen style, digital, fantasy art. ",
            "Veronique Meignaud style, digital, fantasy art.",
            "Veronique Meignaud style, digital, fantasy art.",
            "Wassily Kandinsky, abstract art.",
            "Vincent van Gogh, impressionist style.",
            "Salvador Dali, surrealism style",
            "Salvador Dali, surrealism style"
        ];

        public static ImageGenerationOptions GetImageOptionsForCard(MagicCard card, string imageModel)
        {
            var prompt = $"{card.Name}: {card.FlavorText}";
            var artistStyle = Artists[new Random().Next(Artists.Length)];

            if (card.Type == CardType.Creature)
            {
                if (imageModel == Constants.Dalle2ModelName)
                {
                    prompt = $"'An image of {card.Name}, a {card.TypeLine}: {card.FlavorText}. {artistStyle}";
                }

                if (imageModel == Constants.Dalle3ModelName)
                {
                    // Dalle 3 does not like the flavor text in the prompt usually. It leads to a lot of text in the images.
                    prompt = $"An image of {card.Name}, a {card.TypeLine}. {artistStyle}";
                }
            }

            if (card.Type == CardType.Instant ||
                card.Type == CardType.Sorcery ||
                card.Type == CardType.Enchantment ||
                card.Type == CardType.Artifact ||
                card.Type == CardType.Enchantment ||
                card.Type == CardType.Artifact)
            {
                prompt = $"An image of {card.Name}: {card.FlavorText}. {artistStyle}";
            }

            return new ImageGenerationOptions()
            {
                Prompt = prompt,
                Style = artistStyle,
                Model = imageModel,
                Size = StaticValues.ImageStatics.Size.Size1024,
            };
        }

        public static async Task<string> GenerateImage(string prompt, string model, string size, string apiKey, ILogger log, Cost cost = null)
        {
            if (model != Constants.Dalle2ModelName && model != Constants.Dalle3ModelName)
            {
                throw new Exception($"Invalid image model: {model}. Expecting '{Constants.Dalle2ModelName}' or '{Constants.Dalle3ModelName}'.");
            }

            var openAIService = new OpenAIService(new OpenAIOptions()
            {
                ApiKey = apiKey
            });

            log.LogInformation($"{model} image prompt: {prompt}");

            if (model == Constants.Dalle2ModelName && prompt.Length > 1000)
            {
                log.LogInformation("Truncating prompt to 1000 characters because this is the dall-e-2 limit.");
                prompt = prompt[..1000];
            }

            if (model == Constants.Dalle3ModelName && prompt.Length > 4000)
            {
                log.LogInformation("Truncating prompt to 4000 characters because this is the dall-e-3 limit.");
                prompt = prompt[..4000];
            }

            var imageResult = await openAIService.Image.CreateImage(new ImageCreateRequest
            {
                Prompt = prompt,
                N = 1,
                Size = size,
                Model = model,
                Quality = model == Constants.Dalle2ModelName ? null : "standard",
                ResponseFormat = StaticValues.ImageStatics.ResponseFormat.Url
            });

            cost?.AddImageCost(size, model);

            if (!imageResult.Successful)
            {
                throw new Exception($"Failed to generate image: {imageResult.Error.Message}");
            }

            return imageResult.Results.First().Url;
        }

        public static async Task<string> GenerateDetailedImagePrompt(ImageGenerationOptions options, string apiKey, ILogger log)
        {
            var openAIService = new OpenAIService(new OpenAIOptions()
            {
                ApiKey = apiKey
            });

            var prompt = @$"Please generate a descriptive description that could be used to depict an image of the following text (the description should reflect the artists style listed in the text). 
The text is the following: {options.Prompt}";

            var result = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages =
                [
                    ChatMessage.FromUser(prompt),
                ],

                Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4o_mini,
            });

            return result.Choices.First().Message.Content;
        }
    }
}
