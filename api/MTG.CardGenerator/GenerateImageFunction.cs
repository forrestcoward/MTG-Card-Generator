using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Images;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GenerateImageFunction
    {
        [FunctionName("GenerateImage")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var userPrompt = (string)req.Query["userPrompt"];
                log?.LogInformation($"User prompt: {userPrompt.Replace("\n", "")}");

                var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                OpenAIAPI api = new OpenAIAPI(new APIAuthentication(apiKey));
                var openAICards = Array.Empty<BasicCard>();

                // Generate an image for each card.
                var stopwatch = Stopwatch.StartNew();
                var response = await api.ImageGenerations.CreateImageAsync(new ImageGenerationRequest()
                {
                    NumOfImages = 1,
                    ResponseFormat = ImageResponseFormat.Url,
                    Size = ImageSize._1024,
                    Prompt = userPrompt,
                });

                var jsonResponse = new Dictionary<string, object>();
                var urls = response.Data.Select(x => x.Url);
                jsonResponse["urls"] = urls;

                stopwatch.Stop();
                log.LogMetric("CreateImageAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds);

                var json = JsonConvert.SerializeObject(urls);
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
