using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using OpenAI.ObjectModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GenerateImageFunction
    {
        [FunctionName("GenerateImage")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static Task<IActionResult> GenerateImage([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var userPrompt = (string)req.Query["userPrompt"];
                userPrompt = WebUtility.UrlDecode(userPrompt);
                log?.LogInformation($"User prompt: {userPrompt.Replace("\n", "")}");
                var apiKey = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);
                var stopwatch = Stopwatch.StartNew();
                var url = ImageGenerator.GenerateImage(userPrompt, Constants.Dalle3ModelName, StaticValues.ImageStatics.Size.Size1024, apiKey, log).Result;
                stopwatch.Stop();
                log.LogMetric("CreateImageAsync_DurationSeconds", stopwatch.Elapsed.TotalSeconds);

                var json = JsonConvert.SerializeObject(new[] {url});
                return Task.FromResult<IActionResult>(new OkObjectResult(json));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Unexpected exception: {exception}";
                log?.LogError(exception, errorMessage);
                return Task.FromResult<IActionResult>(new ContentResult
                {
                    StatusCode = 500,
                    Content = errorMessage,
                });
            }
        }

        [FunctionName("GetImagePrompt")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> GetImagePrompt([HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = null)] HttpRequest req, ILogger log)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                string requestBody;
                using (var streamReader = new StreamReader(req.Body))
                {
                    requestBody = await streamReader.ReadToEndAsync();
                }

                MagicCard card;
                try
                {
                    card = JsonConvert.DeserializeObject<MagicCard>(requestBody);
                }
                catch (Exception e) when (e is JsonException || e is JsonReaderException)
                {
                    var errorMessage = $"Could not parse request body to type MagicCard.";
                    log?.LogError(errorMessage);
                    return await Task.FromResult<IActionResult>(new ContentResult
                    {
                        StatusCode = 400,
                        Content = errorMessage,
                    });
                }

                var options = ImageGenerator.GetImageOptionsForCard(card, Constants.Dalle3ModelName);
                var apiKey = Environment.GetEnvironmentVariable(Constants.OpenAIApiKey);
                var prompt = await ImageGenerator.GenerateDetailedImagePrompt(options, apiKey, log);

                stopwatch.Stop();
                log.LogMetric("GetImagePrompt_DurationSeconds", stopwatch.Elapsed.TotalSeconds);

                var response = new Dictionary<string, object>()
                {
                    {
                        "suggestedPrompt", prompt
                    }
                };

                var json = JsonConvert.SerializeObject(response);
                return await Task.FromResult<IActionResult>(new OkObjectResult(response));
            }
            catch (Exception exception)
            {
                var errorMessage = $"Unexpected exception: {exception}";
                log?.LogError(exception, errorMessage);
                return await Task.FromResult<IActionResult>(new ContentResult
                {
                    StatusCode = 500,
                    Content = errorMessage,
                });
            }
        }
    }
}
