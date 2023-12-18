using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        internal class GetTopCardsFunctionResponse
        {
            [JsonProperty("cards")]
            public List<CardGenerationRecord> Cards { get; set; }
        }

        internal class GetCardToRateFunctionResponse
        {
            [JsonProperty("cards")]
            public CardGenerationRecord[] Cards { get; set; }
        }

        [FunctionName("GetCardToRate")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunGetCardToRate([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var cardsClient = new CardsClient(log);
                var card = await cardsClient.GetCardToRate(log);

                var json = JsonConvert.SerializeObject(new GetCardToRateFunctionResponse() { Cards = new[] { card } });

                log.LogMetric("GetCardToRate_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                    properties: new Dictionary<string, object>()
                    {
                        { "cardId", card.id },
                    });

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
            var stopwatch = Stopwatch.StartNew();
            var cardId = (string)req.Query["cardId"];
            var ratingString = (string)req.Query["rating"];

            var userSubject = req.GetUserSubject(log);
            if (string.IsNullOrWhiteSpace(userSubject))
            {
                return new UnauthorizedResult();
            }

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
                var cardsClient = new CardsClient(log);
                var usersClient = new UsersClient(log);
                var card = await cardsClient.GetMagicCard(cardId);

                if (card == null)
                {
                    return new ContentResult
                    {
                        StatusCode = 400,
                        Content = $"Parameter 'cardId' was not valid or did not exist.",
                    };
                }

                var updateTask = usersClient.UserRatedCard(userSubject);
                await cardsClient.SetRatingResult(card, rating);
                await updateTask;
                var updatedCard = await cardsClient.GetMagicCard(cardId);
                var json = JsonConvert.SerializeObject(new RateCardFunctionResponse() { Rating = updatedCard.rating });

                log.LogMetric("RateCard_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                    properties: new Dictionary<string, object>()
                    {
                        { "rating", rating },
                        { "cardId", card.id },
                        { "userSubject", userSubject },
                    });

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

        [FunctionName("GetTopCards")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunTopCards([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var cardsClient = new CardsClient(log);
                var topCards = (await cardsClient.GetTopCards(top: 50, requiredNumberOfVotes: 2)).ToList();
                var json = JsonConvert.SerializeObject(new GetTopCardsFunctionResponse() { Cards = topCards });
                log.LogMetric("GetTopCards_DurationSeconds", stopwatch.Elapsed.TotalSeconds);
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
