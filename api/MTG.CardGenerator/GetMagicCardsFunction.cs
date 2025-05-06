using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using System.Net;

namespace MTG.CardGenerator
{
    public class GetMagicCardsFunction
    {
        [Function("GetMagicCardByName")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> RunGetMagicCardByName([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<GetMagicCardsFunction>();
            var cardName = req.GetQueryParam("cardName");
            var cardsClient = new CardsClient(log);
            var records = await cardsClient.GetMagicCardsByName(cardName);
            return await req.CreateJsonResponse(HttpStatusCode.OK, APIResponses.GetCardsResponse(records));
        }

        [Function("GetMagicCard")]
        public static async Task<HttpResponseData> RunGetMagicCard([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<GetMagicCardsFunction>();
            var cardId = req.GetQueryParam("cardId");
            var cardsClient = new CardsClient(log);
            var record = await cardsClient.GetMagicCardRecord(cardId);
            return await req.CreateJsonResponse(HttpStatusCode.OK, APIResponses.GetCardResponse(record));
        }

        [Function("GetMagicCards")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<GetMagicCardsFunction>();
            var userSubject = req.GetUserSubject(log);
            if (string.IsNullOrWhiteSpace(userSubject))
            {
                return req.CreateResponse(HttpStatusCode.Unauthorized);
            }

            var cardsClient = new CardsClient(log);
            var userCards = await cardsClient.GetUsersMagicCards(userSubject);
            log.LogInformation($"Found {userCards.Count} cards for user '{userSubject}'.");

            log.LogMetric("GetMagicCards_Success", 1, new Dictionary<string, object>()
            {
                { "userSubject", userSubject }
            });

            return await req.CreateJsonResponse(HttpStatusCode.OK, APIResponses.GetCardsResponse(userCards));
        }
    }
}
