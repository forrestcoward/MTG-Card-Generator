using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GetUserFunction
    {
        internal class GetuserFunctionResponse
        {
            [JsonProperty("user")]
            public User User { get; set; }
        }

        [FunctionName("GetUser")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var userSubject = req.GetUserSubject(log);
                if (string.IsNullOrWhiteSpace(userSubject))
                {
                    return new UnauthorizedResult();
                }

                var databaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                var cosmosClient = new CosmosClient(databaseId, Constants.CosmosDBUsersCollectionName, log);
                var user = await cosmosClient.GetUserRecord(userSubject);
                var json = JsonConvert.SerializeObject(new GetuserFunctionResponse() { User = user });
                return new OkObjectResult(json);
            }
            catch (Exception ex)
            {
                log?.LogError(ex, ex.Message);
                return new ContentResult
                {
                    StatusCode = 500,
                    Content = ex.Message,
                };
            }
        }
    }
}
