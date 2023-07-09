using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GetMagicCardsFunction
    {
        internal class GetMagicCardFunctionResponse
        {
            [JsonProperty("cards")]
            public MagicCard[] Cards { get; set; }
        }

        [FunctionName("GetMagicCards")]
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
                var collectionId = Extensions.GetSettingOrThrow(Constants.CosmosDBCollectionId);
                var cosmosClient = new CosmosClient(databaseId, collectionId, log);
                var userCards = await cosmosClient.GetMagicCards<MagicCard>(userSubject);
                log.LogInformation($"Found {userCards.Count} cards for user '{userSubject}'.");

                var generatedCards = userCards.Select(x => x.magicCards.FirstOrDefault()).ToArray();
                var json = JsonConvert.SerializeObject(new GetMagicCardFunctionResponse() { Cards = generatedCards });
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
