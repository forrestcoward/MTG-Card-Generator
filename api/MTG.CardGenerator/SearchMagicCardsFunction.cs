﻿using Azure;
using Azure.Search.Documents.Indexes;
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
using System.Linq;
using System.Threading.Tasks;

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
#pragma warning disable IDE1006 // Naming Styles
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
#pragma warning restore IDE1006 // Naming Styles
        }

        [FunctionName("SearchMagicCards")]
        [FunctionAuthorize(Policy = Constants.APIAuthorizationScope)]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                var query = (string)req.Query["query"];

                if (string.IsNullOrWhiteSpace(query))
                {
                    return new BadRequestObjectResult($"Parameter 'query' is required.");
                }

                var cogSearchEndpoint = Extensions.GetSettingOrThrow(Constants.CognitiveSearchEndpoint);
                var cogSearchKey = Extensions.GetSettingOrThrow(Constants.CognitiveSearchConnectionKey);
                var indexName = Extensions.GetSettingOrThrow(Constants.CognitiveSearchCardSearchIndexName);
                var cosmosDatabaseId = Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId);

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

                // var generatedCards = cards.Select(x => x.MagicCards.FirstOrDefault()).Take(30).ToArray();
                // var json = JsonConvert.SerializeObject(new SearchMagicCardsFunctionResponse() { Cards = generatedCards.Select(x => new MagicCardResponse(x)).ToArray() });
                return new OkObjectResult("");
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
