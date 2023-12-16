using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using User = MTG.CardGenerator.Models.User;

namespace MTG.CardGenerator.CosmosClients
{
    /// <summary>
    /// Operations on the 'users' collection.
    /// </summary>
    public class UsersClient : BaseCosmosClient
    {
        public UsersClient(ILogger logger = null) : base(Extensions.GetSettingOrThrow(Constants.CosmosDBDatabaseId), Constants.CosmosDBUsersCollectionName, logger: logger)
        {
        }

        public async Task<User> GetUserRecord(string userSubject)
        {
            var queryDefinition = new QueryDefinitionWrapper(
                @"SELECT TOP 1 * FROM c WHERE c.userSubject = @userSubject")
            .WithParameter("@userSubject", userSubject);

            User user = (await QueryCosmosDB<User>(queryDefinition.QueryDefinition)).FirstOrDefault();
            return user;
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
