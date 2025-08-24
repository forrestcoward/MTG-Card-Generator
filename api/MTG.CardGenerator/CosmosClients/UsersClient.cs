using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using User = MTG.CardGenerator.Models.User;

namespace MTG.CardGenerator.CosmosClients
{
    /// <summary>
    /// Operations on the 'users' collection.
    /// </summary>
    public class UsersClient(ILogger logger = null) : BaseCosmosClient(Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId), Constants.CosmosDBUsersCollectionName, logger: logger)
    {
        private const int NumberOfConcurrencyRetryAttempts = 3;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(100);

        public async Task<User> GetUserRecord(string userSubject)
        {
            var queryDefinition = new QueryDefinitionWrapper(
                @"SELECT TOP 1 * FROM c WHERE c.userSubject = @userSubject")
            .WithParameter("@userSubject", userSubject);

            User user = (await QueryCosmosDB<User>(queryDefinition.QueryDefinition)).FirstOrDefault();
            return user;
        }

        public async Task<ItemResponse<User>> SetUserOpenAIAPIKey(string userSubject, string openAIAPIkey)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Set("/openAIApiKey", openAIAPIkey),
            };

            return await PatchDocument<User>(userSubject, userSubject, operations);
        }

        public async Task<ItemResponse<User>> UserGeneratedCardOperation(User user, string userSubject, int numberOfNewCards, Cost cost)
        {
            return await ExecutePatchWithConflictRetry(async (currentUser) =>
            {
                var operations = new List<PatchOperation>
                {
                    PatchOperation.Increment("/totalCostOfCardsGenerated", cost.CostToWebsiteApiKeys),
                    PatchOperation.Increment("/numberOfCardsGenerated", numberOfNewCards),
                    PatchOperation.Add("/lastActiveTime", DateTime.Now.ToUniversalTime())
                };

                var today = DateTime.Now.ToUniversalTime().Date;
                if (currentUser.LastActiveTime?.Date != today)
                {
                    // Reset daily spend and free card count at start of new day.
                    operations.Add(PatchOperation.Set("/totalCostOfCardsGeneratedToday", 0.0));
                    operations.Add(PatchOperation.Set("/numberOfFreeCardsGeneratedToday", 0));
                }

                operations.Add(PatchOperation.Increment("/totalCostOfCardsGeneratedToday", cost.CostToWebsiteApiKeys));
                operations.Add(PatchOperation.Increment("/numberOfFreeCardsGeneratedToday", numberOfNewCards));

                return await PatchDocument<User>(userSubject, userSubject, operations, currentUser.ETag);
            }, user, userSubject);
        }

        public async Task<ItemResponse<User>> UserGeneratedImageOperation(User user, string userSubject, Cost cost)
        {
            return await ExecutePatchWithConflictRetry(async (currentUser) =>
            {
                var operations = new List<PatchOperation>
                {
                    PatchOperation.Add("/lastActiveTime", DateTime.Now.ToUniversalTime()),
                    PatchOperation.Increment("/totalCostOfCardsGenerated", cost.CostToWebsiteApiKeys),
                };

                var today = DateTime.Now.ToUniversalTime().Date;
                if (currentUser.LastActiveTime?.Date != today)
                {
                    // Reset daily spend and free image count at start of new day.
                    operations.Add(PatchOperation.Set("/totalCostOfCardsGeneratedToday", 0.0));
                }

                operations.Add(PatchOperation.Increment("/totalCostOfCardsGeneratedToday", cost.CostToWebsiteApiKeys));

                return await PatchDocument<User>(userSubject, userSubject, operations, currentUser.ETag);
            }, user, userSubject);
        }

        /// <summary>
        /// Executes a patch operation with automatic retry on concurrency conflicts.
        /// </summary>
        private async Task<ItemResponse<User>> ExecutePatchWithConflictRetry(Func<User, Task<ItemResponse<User>>> patchOperation, User initialUser, string userSubject)
        {
            var currentUser = initialUser;

            for (int attempt = 0; attempt < NumberOfConcurrencyRetryAttempts; attempt++)
            {
                try
                {
                    return await patchOperation(currentUser);
                }
                catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
                {
                    if (attempt == NumberOfConcurrencyRetryAttempts - 1)
                    {
                        throw;
                    }

                    await Task.Delay(RetryDelay);

                    // Refetch the current user document to get the latest etag.
                    currentUser = await GetUserRecord(userSubject);
                    if (currentUser == null)
                    {
                        throw new InvalidOperationException($"User document '{userSubject}' not found during retry");
                    }
                }
            }

            throw new InvalidOperationException();
        }

        // Helper to reset daily spend (can be called at login or start of day).
        public async Task ResetUserDailySpend(string userSubject)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Set("/totalCostOfCardsGeneratedToday", 0.0),
                PatchOperation.Set("/numberOfFreeCardsGeneratedToday", 0)
            };
            await PatchDocument<User>(userSubject, userSubject, operations);
        }

        // Helper to update daily spend limit.
        public async Task UpdateDailySpendLimit(string userSubject, double dailySpendLimit)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Set("/dailySpendLimit", dailySpendLimit)
            };
            await PatchDocument<User>(userSubject, userSubject, operations);
        }

        public async Task<ItemResponse<User>> UserRatedCard(string userSubject)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Increment("/numberOfCardsRated", 1),
            };

            return await PatchDocument<User>(userSubject, userSubject, operations);
        }

        public async Task SetUserAsAdmin(string userSubject)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Set("/isAdmin", true),
            };

            await PatchDocument<User>(userSubject, userSubject, operations);
        }

        public async Task UpdateAllowedFreeCardGenerations(string userSubject, int allowedFreeGenerations)
        {
            var operations = new List<PatchOperation>
            {
                PatchOperation.Set("/allowedFreeCardGenerationsPerDay", allowedFreeGenerations),
            };

            await PatchDocument<User>(userSubject, userSubject, operations);
        }
    }
}
