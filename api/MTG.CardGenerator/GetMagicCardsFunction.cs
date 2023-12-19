using DarkLoop.Azure.Functions.Authorize;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MTG.CardGenerator
{
    public static class GetMagicCardsFunction
    {
        [FunctionName("GetMagicCardByName")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunGetMagicCardByName([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var cardName = (string)req.Query["cardName"];
                var cardsClient = new CardsClient(log);
                var records = await cardsClient.GetMagicCardsByName(cardName);
                return new OkObjectResult(APIResponses.GetCardsResponse(records));
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

        [FunctionName("GetMagicCard")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> RunGetMagicCard([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var cardId = (string)req.Query["cardId"];
                var cardsClient = new CardsClient(log);
                var record = await cardsClient.GetMagicCard(cardId);
                return new OkObjectResult(APIResponses.GetCardResponse(record));
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

                var cardsClient = new CardsClient(log);
                var userCards = await cardsClient.GetUsersMagicCards(userSubject);
                log.LogInformation($"Found {userCards.Count} cards for user '{userSubject}'.");

                log.LogMetric("GetMagicCards_Success", 1, new Dictionary<string, object>()
                {
                    { "userSubject", req.GetUserSubject() }
                });

                return new OkObjectResult(APIResponses.GetCardsResponse(userCards));
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
