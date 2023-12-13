using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    // Not finished "Card Battle" feature.
    public static class CardBattleFunctions
    {
        internal class CardBattleFunctionResponse
        {
            [JsonProperty("cards")]
            public List<CardGenerationRecord> Cards { get; set; }
        }

        internal class CardBattleLeaderboardResponse
        {
            [JsonProperty("cards")]
            public List<CardGenerationRecord> Cards { get; set; }
        }

        internal class CardBattleResultResponse
        {
        }

        [FunctionName("CardBattleLeaderboard")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunCardBattleLeaderboard([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var winnerId = (string)req.Query["winnerId"];
            var loserId = (string)req.Query["loserId"];

            try
            {
                var databaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                var cosmosClient = new CosmosClient(databaseId, Constants.CosmosDBCardsCollectionName, log);
                var leaders = await cosmosClient.GetCardBattleLeaders();
                var json = JsonConvert.SerializeObject(new CardBattleLeaderboardResponse() { Cards = leaders });
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

        [FunctionName("CardBattleResult")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunCardBattleVictory([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var winnerId = (string)req.Query["winnerId"];
            var loserId = (string)req.Query["loserId"];

            try
            {
                var databaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                var cosmosClient = new CosmosClient(databaseId, Constants.CosmosDBCardsCollectionName, log);
                await cosmosClient.SetCardBattleResult(winnerId, loserId);
                var json = JsonConvert.SerializeObject(new CardBattleResultResponse() { });
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

        [FunctionName("GenerateCardBattle")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> GenerateCardBattle([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var databaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                var cosmosClient = new CosmosClient(databaseId, Constants.CosmosDBCardsCollectionName, log);
                var cards = await cosmosClient.GetRandomDocuments<CardGenerationRecord>(2);

                var json = JsonConvert.SerializeObject(new CardBattleFunctionResponse() { Cards = cards });
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
