using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;

namespace MTG.CardGenerator
{
    public class CardRatingFunctions
    {
        public class CardRatingFunctionResponse
        {
            [JsonProperty("rating")]
            public CardRating Rating;
        }

        [Function("GetCardToRate")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> RunGetCardToRate([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<CardRatingFunctions>();
            var stopwatch = Stopwatch.StartNew();
            var cardsClient = new CardsClient(log);
            var card = await cardsClient.GetCardToRate(log);

            log.LogMetric("GetCardToRate_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                properties: new Dictionary<string, object>()
                {
                    { "cardId", card.Id },
                });

            return await req.CreateJsonResponse(HttpStatusCode.OK, APIResponses.GetCardResponse(card));
        }

        [Function("RateCard")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> RunRateCardFunction([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<CardRatingFunctions>();
            var stopwatch = Stopwatch.StartNew();

            var cardId = req.GetQueryParam("cardId");
            var ratingString = req.GetQueryParam("rating");

            var userSubject = req.GetUserSubject(log);
            if (string.IsNullOrWhiteSpace(userSubject))
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            if (!int.TryParse(ratingString, out int rating))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter 'rating' is not an integer.");
            }

            if (rating <= 0 || rating > 5)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter 'rating' must be between 1 and 5.");
            }

            var cardsClient = new CardsClient(log);
            var usersClient = new UsersClient(log);
            var card = await cardsClient.GetMagicCardRecord(cardId);

            if (card == null)
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter 'cardId' was not valid or did not exist.");
            }

            var updateTask = usersClient.UserRatedCard(userSubject);
            await cardsClient.SetRatingResult(card, rating);
            await updateTask;
            var updatedCard = await cardsClient.GetMagicCardRecord(cardId);
            var json = JsonConvert.SerializeObject(new CardRatingFunctionResponse() { Rating = updatedCard.Rating });

            log.LogMetric("RateCard_DurationSeconds", stopwatch.Elapsed.TotalSeconds,
                properties: new Dictionary<string, object>()
                {
                    { "rating", rating },
                    { "cardId", card.Id },
                    { "userSubject", userSubject },
                });

            return await req.CreateJsonResponse(HttpStatusCode.OK, json);

        }

        [Function("GetTopCards")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> RunTopCards([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<CardRatingFunctions>();
            var stopwatch = Stopwatch.StartNew();
            var cardsClient = new CardsClient(log);
            var topCards = (await cardsClient.GetTopCards(top: 100, requiredNumberOfVotes: 2)).ToList();
            log.LogMetric("GetTopCards_DurationSeconds", stopwatch.Elapsed.TotalSeconds);
            return await req.CreateJsonResponse(HttpStatusCode.OK, APIResponses.GetCardsResponse(topCards));
        }
    }
}
