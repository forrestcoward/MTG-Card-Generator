using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.ObjectModels;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GenerateImageFunction
    {
        [FunctionName("GenerateImage")]
        [FunctionAuthorize]
        public static Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var userPrompt = (string)req.Query["userPrompt"];
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
    }
}
