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
    public static class CardRatingFunctions
    {
        internal class RateCardFunctionResponse
        {
            [JsonProperty("rating")]
            public CardRating Rating;
        }

        internal class TopCardsFunctionResponse
        {
            [JsonProperty("cards")]
            public List<CardGenerationRecord> Cards { get; set; }
        }

        internal class GetRandomCardFunctionResponse
        {
            [JsonProperty("cards")]
            public CardGenerationRecord[] Cards { get; set; }
        }

        [FunctionName("GetRandomCard")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunGetRandomCard([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var databaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                var cosmosClient = new CosmosClient(databaseId, Constants.CosmosDBCardsCollectionName, log);

                // var cards = await cosmosClient.GetRandomDocuments<CardGenerationRecord>(1);
                var card = await cosmosClient.GetRandomCardRecord(log);

                var json = JsonConvert.SerializeObject(new GetRandomCardFunctionResponse() { Cards = new[] { card } });
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

        [FunctionName("RateCard")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunRateCardFunction([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var cardId = (string)req.Query["cardId"];
            var ratingString = (string)req.Query["rating"];

            if (!int.TryParse(ratingString, out int rating))
            {
                return new ContentResult
                {
                    StatusCode = 400,
                    Content = $"Parameter 'rating' is not an integer.",
                };
            }

            if (rating <= 0 || rating > 5)
            {
                return new ContentResult
                {
                    StatusCode = 400,
                    Content = $"Parameter 'rating' must be between 1 and 5.",
                };
            }

            try
            {
                var databaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                var cosmosClient = new CosmosClient(databaseId, Constants.CosmosDBCardsCollectionName, log);

                var card = await cosmosClient.GetMagicCard(cardId);

                if (card == null)
                {
                    return new ContentResult
                    {
                        StatusCode = 400,
                        Content = $"Parameter 'cardId' was not valid or did not exist.",
                    };
                }

                await cosmosClient.SetRatingResult(card, rating);
                var updatedCard = await cosmosClient.GetMagicCard(cardId);
                var json = JsonConvert.SerializeObject(new RateCardFunctionResponse() { Rating = updatedCard.rating });
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

        [FunctionName("TopCards")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunTopCards([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var databaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);
                var cosmosClient = new CosmosClient(databaseId, Constants.CosmosDBCardsCollectionName, log);
                var topCards = await cosmosClient.GetTopCards();
                var json = JsonConvert.SerializeObject(new TopCardsFunctionResponse() { Cards = topCards });
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
