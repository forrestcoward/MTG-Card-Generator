using Azure;
using Azure.Search.Documents.Indexes;
using DarkLoop.Azure.Functions.Authorization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.CosmosClients;
using MTG.CardGenerator.Models;
using Newtonsoft.Json;
using System.Net;

namespace MTG.CardGenerator
{
    public class SearchMagicCardsFunction
    {
        private class SearchMagicCardsFunctionResponse
        {
            [JsonProperty("cards")]
            public MagicCardResponse[] Cards { get; set; }
        }

        /// <summary>
        /// Represents a Magic card in the search index.
        /// </summary>
        private class CardIndexResult
        {
            public string id { get; set; }
            public string prompt { get; set; }
            public string name { get; set; }
            public string manaCost { get; set; }
            public string type { get; set; }
            public string oracleText { get; set; }
            public string rarity { get; set; }
            public string pt { get; set; }
            public string colorIdentity { get; set; }
            public string explanation { get; set; }
            public string funnyExplanation { get; set; }
        }

        [Function("SearchMagicCards")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, FunctionContext context)
        {
            var log = context.GetLogger<SearchMagicCardsFunction>();

            var query = req.GetQueryParam("query");

            if (string.IsNullOrWhiteSpace(query))
            {
                return await req.CreateErrorResponse(HttpStatusCode.BadRequest, "Parameter 'query' is required.");
            }

            var cogSearchEndpoint = Extensions.GetSettingOrThrow(Constants.CognitiveSearchEndpoint);
            var cogSearchKey = Extensions.GetSettingOrThrow(Constants.CognitiveSearchConnectionKey);
            var indexName = Extensions.GetSettingOrThrow(Constants.CognitiveSearchCardSearchIndexName);

            var searchCredential = new AzureKeyCredential(cogSearchKey);
            var indexClient = new SearchIndexClient(new Uri(cogSearchEndpoint), searchCredential);
            var searchClient = indexClient.GetSearchClient(indexName);

            var results = await searchClient.SearchAsync<CardIndexResult>(query);
            var ids = new List<string>();
            await foreach (var result in results.Value.GetResultsAsync())
            {
                ids.Add(result.Document.id);
            }

            var cardsClient = new CardsClient(log);
            var cards = await cardsClient.GetMagicCardRecords(ids);
            return await req.CreateJsonResponse(HttpStatusCode.OK, string.Empty);
        }
    }
}
