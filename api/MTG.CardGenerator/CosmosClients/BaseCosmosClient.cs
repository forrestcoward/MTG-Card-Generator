using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MTG.CardGenerator.CosmosClients
{
    public static class CosmosConnectionManager
    {
        private static readonly ConcurrentDictionary<string, CosmosClient> cosmosClients = new();

        public static CosmosClient GetCosmosClient(string cosmosEndpointUrl = null, string accessKey = null)
        {
            if (cosmosEndpointUrl == null)
            {
                cosmosEndpointUrl = Environment.GetEnvironmentVariable(Constants.CosmosDBEndpointUrl);
                if (string.IsNullOrWhiteSpace(cosmosEndpointUrl))
                {
                    throw new ArgumentException($"Environment variable '{Constants.CosmosDBEndpointUrl}' is not set or empty.");
                }
            }

            if (accessKey == null)
            {
                accessKey = Environment.GetEnvironmentVariable(Constants.CosmosDBAccessKey);
                if (string.IsNullOrWhiteSpace(accessKey))
                {
                    throw new ArgumentException($"Environment variable '{Constants.CosmosDBAccessKey}' is not set or empty.");
                }
            }

            if (!cosmosClients.ContainsKey(cosmosEndpointUrl))
            {
                cosmosClients[cosmosEndpointUrl] = new CosmosClient(cosmosEndpointUrl, accessKey);
            }

            return cosmosClients[cosmosEndpointUrl];
        }
    }

    public class BaseCosmosClient
    {
        internal CosmosClient CosmosClient { get; private set; }

        internal string DatabaseId { get; private set; }
        private readonly Database database;

        internal string ContainerId { get; private set; }
        private readonly Container container;

        private readonly ILogger log = null;

        public BaseCosmosClient(string databaseId, string containerId, string cosmosEndpointUrl = null, string accessKey = null, ILogger logger = null)
        {
            CosmosClient = CosmosConnectionManager.GetCosmosClient(cosmosEndpointUrl, accessKey);
            DatabaseId = databaseId;
            database = CosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId).Result;
            ContainerId = containerId;
            container = database.CreateContainerIfNotExistsAsync(ContainerId, "/id").Result;
            log = logger;
        }

        internal async Task<T> AddItemToContainerAsync<T>(T item)
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

        internal async Task<int> GetDocumentCount()
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

        internal async Task<List<T>> GetRandomDocuments<T>(int number)
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

        internal async Task<T> GetDocument<T>(string id) where T : class
        {
            ItemResponse<T> response = await container.ReadItemAsync<T>(id, new PartitionKey(id));
            return response.Resource;
        }

        internal async Task<bool> DocumentExists(string id)
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

        internal async Task<ItemResponse<T>> PatchDocument<T>(string id, string partitionkey, List<PatchOperation> patches)
        {
            return await container.PatchItemAsync<T>(id, new PartitionKey(partitionkey), patches);
        }

        internal async Task<List<T>> QueryCosmosDB<T>(QueryDefinition query, int batchNumber = 0, bool throwIfNoResults = false, string queryName = "", bool verbose = false, ILogger log = null)
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
