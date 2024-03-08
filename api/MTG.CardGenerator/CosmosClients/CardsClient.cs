﻿using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MTG.CardGenerator.CosmosClients
{
    /// <summary>
    /// Operations on the 'generated-cards' collection.
    /// </summary>
    internal class CardsClient : BaseCosmosClient
    {
        private static readonly char[] AlphanumericCharacters = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly Random Random = new();

        public CardsClient(ILogger logger = null) : base(Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId), Constants.CosmosDBCardsCollectionName, logger: logger)
        {
        }

        public async Task<List<CardGenerationRecord>> GetUsersMagicCards(string userSubject, int top = 50)
        {
            var queryDefinition = new QueryDefinitionWrapper(@"
                SELECT TOP @top *
                FROM c
                WHERE c.user.userSubject = @userSubject
                ORDER BY c.generationMetadata.timestamp DESC")
            .WithParameter("@userSubject", userSubject)
            .WithParameter("@top", top);

            var userCards = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return userCards;
        }

        public async Task<List<CardGenerationRecord>> GetMagicCardsByName(string name)
        {
            var queryDefinition = new QueryDefinitionWrapper(@"
                SELECT *FROM c
                WHERE c.magicCards[0].name = @name
                ORDER BY c.generationMetadata.timestamp DESC")
            .WithParameter("@name", name);

            var userCards = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return userCards;
        }

        public async Task<CardGenerationRecord> GetCardToRate(ILogger log)
        {
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var param = GetRandomAlphanumericCharacter();
                if (attempt > 20)
                {
                    param = GetRandomAlphanumericCharacter() + GetRandomAlphanumericCharacter();
                }

                // WHERE STARTSWITH(c.magicCards[0].imageUrl, 'https://mtgcardgen')
                var queryDefinition = new QueryDefinitionWrapper(@$"
                    SELECT * FROM c
                    WHERE c.generationMetadata.imageModel = 'dall-e-3'
                    AND c.card.imageUrl != ''
                    AND c.generationMetadata.model = 'gpt-4-1106-preview'
                    AND STARTSWITH(c.id, @param)")
               .WithParameter("@param", param);

                var records = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);

                if (records.Count != 0)
                {
                    log.LogInformation($"Found {records.Count} card records starting with '{param}'.");
                    var randomIndex = Random.Next(0, records.Count);
                    return records[randomIndex];
                }
                else
                {
                    log.LogWarning("No card records found starting with '{param}'. Trying again...", param);
                }
            }

            throw new Exception("Error retrieving random card record.");
        }

        private static string GetRandomAlphanumericCharacter()
        {
            int randomIndex = Random.Next(0, AlphanumericCharacters.Length);
            return AlphanumericCharacters[randomIndex].ToString();
        }

        public async Task<List<CardGenerationRecord>> GetMagicCardRecords(IEnumerable<string> ids)
        {
            ids = ids.Select(x => $"'{x}'");
            var queryDefinition = new QueryDefinitionWrapper(
                @$"SELECT *
                    FROM c
                    WHERE ARRAY_CONTAINS([{string.Join(",", ids)}], c.id)");

            var userCards = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return userCards;
        }

        public async Task<CardGenerationRecord> GetMagicCardRecord(string id)
        {
            return await GetDocument<CardGenerationRecord>(id);
        }

        public async Task SetRatingResult(CardGenerationRecord card, int rating)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Increment("/rating/numberOfVotes", 1),
                PatchOperation.Increment("/rating/totalScore", rating),
                PatchOperation.Add("/rating/mostRecent", DateTime.Now.ToUniversalTime())
            };

            if (card.Rating != null)
            {
                operations.Add(PatchOperation.Set("/rating/averageScore", Math.Round((double)(card.Rating.TotalScore + rating) / (card.Rating.NumberOfVotes + 1), 4)));
            }

            if (card.Rating == null)
            {
                operations.Insert(0, PatchOperation.Add("/rating", new { numberOfVotes = 0, totalScore = 0, averageScore = 0 }));
                operations.Add(PatchOperation.Set("/rating/averageScore", rating));
            }

            await PatchDocument<CardGenerationRecord>(card.Id, card.Id, operations);
        }

        public async Task AddAlternativeImageId(CardGenerationRecord card, string alternativeImageId)
        {
            var operations = new List<PatchOperation>();
            if (card.Card.AlternativeImageIds == null)
            {
                operations.Add(PatchOperation.Set($"/card/alternativeImageIds", new string[] { alternativeImageId }));
            }
            else
            {
                operations.Add(PatchOperation.Add("/card/alternativeImageIds/-", alternativeImageId));
            }

            await PatchDocument<CardGenerationRecord>(card.Id, card.Id, operations);
        }

        public async Task SetCardImageUrl(CardGenerationRecord card, string imageUrl)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Set($"/card/imageUrl", imageUrl)
            };
            await PatchDocument<CardGenerationRecord>(card.Id, card.Id, operations);
        }

        public async Task<List<CardGenerationRecord>> GetTopCards(int top, int requiredNumberOfVotes)
        {
            var queryDefinition = new QueryDefinitionWrapper(@$"
                SELECT *
                FROM c
                WHERE IS_DEFINED(c.rating)
                AND c.rating.numberOfVotes >= @requiredNumberOfVotes
                ORDER BY c.rating.averageScore DESC OFFSET 0 LIMIT @top")
                .WithParameter("@top", top)
                .WithParameter("@requiredNumberOfVotes", requiredNumberOfVotes);

            var leaders = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return leaders;
        }
    }
}
