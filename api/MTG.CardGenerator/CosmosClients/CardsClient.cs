using Microsoft.Azure.Cosmos;
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
                ORDER BY c._ts DESC")
            .WithParameter("@userSubject", userSubject)
            .WithParameter("@top", top);

            var userCards = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return userCards;
        }

        public async Task<CardGenerationRecord> GetRandomCardRecord(ILogger log)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var param = GetRandomAlphanumericCharacter();
                if (attempt < 10)
                {
                    param = GetRandomAlphanumericCharacter() + GetRandomAlphanumericCharacter();
                }

                var queryDefinition = new QueryDefinitionWrapper(@$"
                    SELECT * FROM c
                    WHERE STARTSWITH(c.magicCards[0].imageUrl, 'https://mtgcardgen')
                    AND c.generationMetadata.imageModel = 'dall-e-3'
                    AND c.generationMetadata.model = 'gpt-4-1106-preview'
                    AND STARTSWITH(c.id, @param)")
               .WithParameter("@param", param);

                var records = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);

                if (records.Count != 0)
                {
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

        public async Task<CardGenerationRecord> GetMagicCard(string id)
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

            if (card.rating != null)
            {
                operations.Add(PatchOperation.Set("/rating/averageScore", Math.Round((double)(card.rating.TotalScore + rating) / (card.rating.NumberOfVotes + 1), 4)));
            }

            if (card.rating == null)
            {
                operations.Insert(0, PatchOperation.Add("/rating", new { numberOfVotes = 0, totalScore = 0, averageScore = 0 }));
                operations.Add(PatchOperation.Set("/rating/averageScore", rating));
            }

            await PatchDocument<CardGenerationRecord>(card.id, card.id, operations);
        }

        public async Task<List<CardGenerationRecord>> GetTopCards(int top, int requiredNumberOfVotes)
        {
            var queryDefinition = new QueryDefinitionWrapper(@$"
                SELECT *
                FROM c
                WHERE IS_DEFINED(c.rating)
                AND c.rating.numberOfVotes > @requiredNumberOfVotes
                ORDER BY c.rating.averageScore DESC OFFSET 0 LIMIT @top")
                .WithParameter("@top", top)
                .WithParameter("@requiredNumberOfVotes", requiredNumberOfVotes);

            var leaders = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return leaders;
        }
    }
}
