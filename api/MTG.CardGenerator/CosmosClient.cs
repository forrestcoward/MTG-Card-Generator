using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using MTG.CardGenerator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using User = MTG.CardGenerator.Models.User;

namespace MTG.CardGenerator
{
    public class CosmosClient
    {
        private static readonly Lazy<Microsoft.Azure.Cosmos.CosmosClient> lazyClient = new(InitializeCosmosClient);
        private static Microsoft.Azure.Cosmos.CosmosClient cosmosClient => lazyClient.Value;

        public string DatabaseId { get; private set; }
        private readonly Database database;

        public string ContainerId { get; private set; }
        private readonly Container container;

        private readonly ILogger log = null;

        public CosmosClient(string databaseId, string containerId, ILogger logger = null)
        {
            DatabaseId = databaseId;
            database = cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId).Result;
            ContainerId = containerId;
            container = database.CreateContainerIfNotExistsAsync(ContainerId, "/id").Result;
            log = logger;
        }

        private static Microsoft.Azure.Cosmos.CosmosClient InitializeCosmosClient()
        {
            var endpointUrl = Environment.GetEnvironmentVariable(Constants.CosmosDBEndpointUrl);
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                throw new ArgumentException($"Environment variable '{Constants.CosmosDBEndpointUrl}' is not set or empty.");
            }

            var accessKey = Environment.GetEnvironmentVariable(Constants.CosmosDBAccessKey);
            if (string.IsNullOrWhiteSpace(accessKey))
            {
                throw new ArgumentException($"Environment variable '{Constants.CosmosDBAccessKey}' is not set or empty.");
            }

            return new Microsoft.Azure.Cosmos.CosmosClient(endpointUrl, accessKey);
        }

        public async Task<T> AddItemToContainerAsync<T>(T item)
        {
            try
            {
                T response = await container.CreateItemAsync(item);
                return response;
            }
            catch (CosmosException ce)
            {
                var baseException = ce.GetBaseException();
                log?.LogError($"{ce.StatusCode} error occurred inserting item: {ce.Message}, Inner Exception: {baseException.Message}");
                throw;
            }
        }

        public async Task<int> GetDocumentCount()
        {
            var countQuery = new QueryDefinition("SELECT VALUE COUNT(1) FROM c");
            var countIterator = container.GetItemQueryIterator<int>(countQuery);
            var count = 0;
            while (countIterator.HasMoreResults)
            {
                var countResponse = await countIterator.ReadNextAsync();
                count = countResponse.Resource.First();
            }

            return count;
        }

        public async Task<List<T>> GetRandomDocuments<T>(int number)
        {
            var result = new List<T>();
            var count = await GetDocumentCount();
            var rnd = new Random();

            for (var i = 0; i < number; i++)
            {
                var randomOffset = rnd.Next(count);

                // Fetch a random document.
                var randomDocQuery = new QueryDefinition($"SELECT * FROM c OFFSET {randomOffset} LIMIT 1");
                var randomDocIterator = container.GetItemQueryIterator<T>(randomDocQuery);
                while (randomDocIterator.HasMoreResults)
                {
                    var randomDocResponse = await randomDocIterator.ReadNextAsync();
                    foreach (var item in randomDocResponse)
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        public async Task<CardGenerationRecord> GetRandomCardRecord(ILogger log)
        {
            for (var attempt = 0; attempt < 20; attempt++)
            {
                var param = GetRandomAlphanumericCharacter() + GetRandomAlphanumericCharacter();
                var queryDefinition = new QueryDefinitionWrapper(
                    @$"
SELECT * FROM c
WHERE STARTSWITH(c.magicCards[0].imageUrl, 'https://mtgcardgen')
AND STARTSWITH(c.id, @param)
")
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

        private static readonly char[] AlphanumericCharacters = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
        private static readonly Random Random = new Random();
        private static string GetRandomAlphanumericCharacter()
        {
            int randomIndex = Random.Next(0, AlphanumericCharacters.Length);
            return AlphanumericCharacters[randomIndex].ToString();
        }

        public async Task<List<CardGenerationRecord>> GetUsersMagicCards(string userSubject, int top = 50)
        {
            var queryDefinition = new QueryDefinitionWrapper(
                @"
SELECT TOP @top *
FROM c
WHERE c.user.userSubject = @userSubject
ORDER BY c._ts DESC
")
                .WithParameter("@userSubject", userSubject)
                .WithParameter("@top", top);

            var userCards = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return userCards;
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

        public async Task<User> GetUserRecord(string userSubject)
        {
            var queryDefinition = new QueryDefinitionWrapper(
                @"SELECT TOP 1 * FROM c WHERE c.userSubject = @userSubject")
            .WithParameter("@userSubject", userSubject);

            User user = (await QueryCosmosDB<User>(queryDefinition.QueryDefinition)).FirstOrDefault();
            return user;
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

        public async Task<List<CardGenerationRecord>> GetTopCards()
        {
            var queryDefinition = new QueryDefinitionWrapper(
                @$"
SELECT *
FROM c
WHERE IS_DEFINED(c.rating)
ORDER BY c.rating.averageScore DESC OFFSET 0 LIMIT 50
");

            var leaders = await QueryCosmosDB<CardGenerationRecord>(queryDefinition.QueryDefinition);
            return leaders;
        }

        public async Task<ItemResponse<User>> UpdateUserRecord(User user, string userSubject, int numberOfNewCards, double cardGenerationEstimatedCost, bool userSuppliedApiKey)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Increment("/totalCostOfCardsGenerated", cardGenerationEstimatedCost),
                PatchOperation.Increment("/numberOfCardsGenerated", numberOfNewCards),
                PatchOperation.Add("/lastActiveTime", DateTime.Now.ToUniversalTime())
            };

            if (user.lastActiveTime?.Date != DateTime.Now.ToUniversalTime().Date)
            {
                // If this is the first time the user has generated today, reset their number of free card generations allowed in the day.
                operations.Add(PatchOperation.Replace("/numberOfFreeCardsGeneratedToday", 0));
            }


            if (!userSuppliedApiKey)
            {
                // Otherwise, increment the number of cards generated today.
                operations.Add(PatchOperation.Increment("/numberOfFreeCardsGeneratedToday", numberOfNewCards));
            }

            return await PatchDocument<User>(userSubject, userSubject, operations);
        }

        public async Task<T> GetDocument<T>(string id) where T : class
        {
            ItemResponse<T> response = await container.ReadItemAsync<T>(id, new PartitionKey(id));
            return response.Resource;
        }

        public async Task<bool> DocumentExists(string id)
        {
            try
            {
                // Use point read which is the most efficient way to read a single item by its id with retrieving item content.
                ResponseMessage response = await container.ReadItemStreamAsync(id, new PartitionKey(id));
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        private async Task<ItemResponse<T>> PatchDocument<T>(string id, string partitionkey, List<PatchOperation> patches)
        {
            return await container.PatchItemAsync<T>(id, new PartitionKey(partitionkey), patches);
        }

        private async Task<List<T>> QueryCosmosDB<T>(QueryDefinition query, int batchNumber = 0, bool throwIfNoResults = false, string queryName = "", bool verbose = false, ILogger log = null)
        {
            var watch = Stopwatch.StartNew();
            if (verbose)
            {
                log?.LogInformation($"Running query {query.QueryText}", queryName, batchNumber, verbose);
            }

            double totalCharge = 0;
            var results = new List<T>();
            try
            {
                var queryOptions = new QueryRequestOptions
                {
                    // Turn on to view index usage and recommended indices by Cosmos.
                    PopulateIndexMetrics = false,
                    MaxItemCount = 5000
                };

                var resultSetIterator = container.GetItemQueryIterator<T>(query, continuationToken: null, requestOptions: queryOptions);
                while (resultSetIterator.HasMoreResults)
                {
                    FeedResponse<T> response = await resultSetIterator.ReadNextAsync().ConfigureAwait(true);
                    results.AddRange(response);

                    if (verbose)
                    {
                        log?.LogInformation($"Iterated {response.Count} new records. Request Charge: {response.RequestCharge}", queryName, batchNumber, verbose);
                    }
                    totalCharge += response.RequestCharge;

                    if (verbose && resultSetIterator.HasMoreResults && !string.IsNullOrWhiteSpace(response.IndexMetrics))
                    {
                        // Very verbose, keep off for now unless needed.
                        log.LogInformation(response.IndexMetrics, queryName, batchNumber, verbose);
                    }
                }
            }
            catch (Exception e)
            {
                log?.LogError(e.ToString());
                throw;
            }

            if (results.Count == 0 && throwIfNoResults)
            {
                var errorMessage = $"No results on {container.Id} for the given query: {query.QueryText}";
                var subActivity = 0;
                var charge = 0;
                throw new CosmosException(errorMessage, HttpStatusCode.NotFound, subActivity, container.Id, charge);
            }

            if (verbose)
            {
                log?.LogInformation($"Query completed in {watch.Elapsed} and had {results.Count} results. Total charge: {totalCharge}");
            }

            return results;
        }
    }
}
