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
            "Rebecca Guay style, digital, fantasy art.",
            "Seb Mckinnon style, digital, fantasy art.",
            "Terese Nielsen style, digital, fantasy art.",
            "Christopher Rush style, digital, fantasy art.",
            "Veronique Meignaud style, digital, fantasy art.",
            "Adam Paquette style, digital, fantasy art.",
            "John Avon style, digital, fantasy art.",
            "Johannes Voss style, digital, fantasy art.",
            "Willian Munny style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Greg Kutkowski style, digital, fantasy art.",
            "Terese Nielsen style, digital, fantasy art. ",
            "Wassily Kandinsky, abstract, fantasy art.",
            "Vincent van Gogh, impressionist, fantasy style.",
            "Salvador Dali, surrealism, fantasy style",
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

            if (card.Type == CardType.Instant || card.Type == CardType.Sorcery || card.Type == CardType.Enchantment || card.Type == CardType.Artifact || card.Type == CardType.Enchantment || card.Type == CardType.Artifact)
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

        /// <summary>
        /// Generates an image using the specified model and prompt.
        /// </summary>
        /// <param name="prompt">the prompt</param>
        /// <param name="model">the model</param>
        /// <param name="size">the model size, if using DALL-E</param>
        /// <param name="openAIApiKey">the OpenAI API key</param>
        /// <param name="replicateApiKey">the Replicate API key</param>
        /// <param name="cost">the cost object (updated with charge to generate the image)</param>
        /// <param name="log">the logger object</param>
        /// <returns>the image url</returns>
        public static async Task<string> GenerateImage(string prompt, string model, string size, APIKey openAIApiKey, APIKey replicateApiKey, Cost cost, ILogger log)
        {
            log.LogInformation($"{model} image prompt: {prompt}");
            var operationName = "Generate Card Image";

            if (model == Constants.BlackForestLabsFluxSchnell)
            {
                var replicateClient = new ReplicateApiClient(replicateApiKey.Key);
                var input = new
                {
                    prompt = prompt,
                    num_outputs = 1,
                    num_inference_steps = 4,
                    output_format = "png",
                    aspect_ratio = "1:1",
                    output_quality = 90,
                    disable_safety_checker = false,
                    go_fast = true
                };
                var outputUrl = await replicateClient.RunPredictionAsync("black-forest-labs/flux-schnell", input);
                cost.AddImageCost(size, model, wasUserCharged: replicateApiKey.IsUserSupplied, operationName: operationName);
                return outputUrl;
            }

            if (model == Constants.StableDiffusion35MediumModelName)
            {
                var replicateClient = new ReplicateApiClient(replicateApiKey.Key);
                var input = new
                {
                    prompt = prompt,
                    cfg = 5,
                    prompt_strength = .85,
                    steps = 40,
                    output_format = "png",
                    aspect_ratio = "1:1",
                    output_quality = 90
                };
                var outputUrl = await replicateClient.RunPredictionAsync("stability-ai/stable-diffusion-3.5-medium", input);
                cost.AddImageCost(size, model, wasUserCharged: replicateApiKey.IsUserSupplied, operationName: operationName);
                return outputUrl;
            }

            if (model == Constants.GoogleImagen3FastModelName)
            {
                var replicateClient = new ReplicateApiClient(replicateApiKey.Key);
                var input = new
                {
                    prompt = prompt,
                    num_inference_steps = 4,
                    num_outputs = 1,
                    output_format = "png",
                    aspect_ratio = "1:1",
                    disable_safety_checker = true,
                    go_fast = true,
                    megapixels = 1
                };

                var outputUrl = await replicateClient.RunPredictionAsync("google/imagen-3-fast", input);

                cost.AddImageCost(size, model, wasUserCharged: replicateApiKey.IsUserSupplied, operationName: operationName);
                return outputUrl;
            }

            if (model != Constants.Dalle2ModelName && model != Constants.Dalle3ModelName)
            {
                throw new Exception($"Invalid image model: {model}. Expecting '{Constants.Dalle2ModelName}', '{Constants.Dalle3ModelName}', or '{Constants.GoogleImagen3FastModelName}'.");
            }

            var openAIService = new OpenAIService(new OpenAIOptions() { ApiKey = openAIApiKey.Key });

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

            cost.AddImageCost(size, model, wasUserCharged: openAIApiKey.IsUserSupplied, operationName: operationName);

            if (!imageResult.Successful)
            {
                // TODO: Error.Message is often empty. This happens when the prompt is deemed inappropriate.
                throw new Exception($"Failed to generate image: {imageResult.Error.Message}");
            }

            return imageResult.Results.First().Url;
        }

        public static async Task<string> GenerateDetailedImagePrompt(ImageGenerationOptions options, APIKey openAIApiKey, Cost cost, ILogger log)
        {
            var openAIService = new OpenAIService(new OpenAIOptions()
            {
                ApiKey = openAIApiKey.Key
            });

            var prompt = @$"Please generate a descriptive description that could be used to depict an image of the following text (the description should reflect the artists style listed in the text). 
The text is the following: {options.Prompt}

Only output the description, nothing else.";

            var result = await openAIService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
            {
                Messages =
                [
                    ChatMessage.FromUser(prompt),
                ],

                Model = Betalgo.Ranul.OpenAI.ObjectModels.Models.Gpt_4_1,
            });

            cost.AddChatCost(result, wasUserCharged: openAIApiKey.IsUserSupplied, operationName: "Generated Detailed Image Prompt", log);
            return result.Choices.First().Message.Content;
        }
    }
}
